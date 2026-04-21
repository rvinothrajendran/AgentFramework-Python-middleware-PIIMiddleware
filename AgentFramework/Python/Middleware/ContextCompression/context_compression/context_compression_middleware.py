from typing import Any, Callable, Awaitable, List, Optional
import logging

from agent_framework import ChatMiddleware, ChatContext, Message
import tiktoken


class TokenThresholdReachedError(RuntimeError):
    """Raised when the token threshold is reached and the caller chooses to stop."""
    pass


class ContextCompressionMiddleware(ChatMiddleware):
    """
    Compresses old conversation history when the token threshold is reached.

    Flow:
        Before LLM call:
            - inspect messages
            - count tokens
            - compress old history if needed
            - inject compacted history

        Then:
            await call_next()
    """

    def __init__(
        self,
        llm_client,
        max_tokens: int = 8000,
        trigger_ratio: float = 0.80,
        keep_last_messages: int = 8,
        model_encoding: str = "cl100k_base",
        on_threshold_reached: Optional[Callable[[dict], bool]] = None,
        logger: Optional[logging.Logger] = None,
    ):
        """
        on_threshold_reached: optional callback called when the token threshold is hit.
            Receives a dict with keys: tokens_used, max_tokens, trigger_tokens.
            Return True  → allow compression to proceed (default behaviour).
            Return False → stop the request and raise TokenThresholdReachedError.
        logger: optional Logger instance. If not provided, a default logger is created.
        """
        self.llm_client = llm_client
        self.max_tokens = max_tokens
        self.trigger_tokens = int(max_tokens * trigger_ratio)
        self.keep_last_messages = keep_last_messages
        self.encoder = tiktoken.get_encoding(model_encoding)
        self.last_usage: dict | None = None
        self.on_threshold_reached = on_threshold_reached
        self.logger = logger or logging.getLogger(__name__)

    async def process(self, context: ChatContext, next: Callable[[], Awaitable[None]]) -> None:

        self.logger.debug("ContextCompressionMiddleware: before LLM call")

        messages = self._get_messages(context)

        if messages:
            total_tokens = self._count_tokens(messages)
            self.logger.debug("Token count before call: %d / %d", total_tokens, self.max_tokens)

            if total_tokens >= self.trigger_tokens:
                self.logger.warning(
                    "Token threshold reached — compressing history "
                    "(tokens_used=%d, max=%d, trigger=%d)",
                    total_tokens, self.max_tokens, self.trigger_tokens,
                )

                info = {
                    "tokens_used": total_tokens,
                    "max_tokens": self.max_tokens,
                    "trigger_tokens": self.trigger_tokens,
                }

                if self.on_threshold_reached is not None:
                    allow = self.on_threshold_reached(info)
                    if not allow:
                        self.logger.error(
                            "Request blocked by on_threshold_reached callback "
                            "(tokens_used=%d, max=%d)",
                            total_tokens, self.max_tokens,
                        )
                        raise TokenThresholdReachedError(
                            f"Token threshold reached: {total_tokens} / {self.max_tokens}. "
                            "Request blocked by on_threshold_reached callback."
                        )

                compressed = await self._compress_messages(messages)
                self._set_messages(context, compressed)
                self.logger.info("History compressed to %d messages", len(compressed))

        # pass to LLM
        if getattr(context, "stream", False):
            await self._attach_streaming_hooks(context)
            await next()
            return

        await next()

        # Track token usage from the LLM response
        usage = self._extract_response_usage(context)
        if usage:
            self._track_response_tokens(usage)

    # --------------------------------------------------
    # Message Accessors
    # --------------------------------------------------

    def _get_messages(self, context) -> list:
        return list(context.messages)

    def _set_messages(self, context, messages: list) -> None:
        context.messages[:] = messages

    # --------------------------------------------------
    # Response Token Tracking
    # --------------------------------------------------

    def _extract_response_usage(self, context) -> dict | None:
        """Extract token usage details from the LLM response stored on context."""
        result = getattr(context, "result", None)
        if not result:
            return None
        usage = getattr(result, "usage_details", None)
        if not usage:
            return None
        return dict(usage)

    def _track_response_tokens(self, usage: dict) -> None:
        """Store and log token usage returned by the LLM after the call."""
        input_tokens = usage.get("input_token_count")
        output_tokens = usage.get("output_token_count")
        total_tokens = int(usage.get("total_token_count") or 0)
        if not total_tokens:
            total_tokens = int(input_tokens or 0) + int(output_tokens or 0)
        self.last_usage = {
            **usage,
            "input_token_count": input_tokens,
            "output_token_count": output_tokens,
            "total_token_count": total_tokens,
        }
        self.logger.info(
            "LLM usage — input=%s, output=%s, total=%s",
            input_tokens, output_tokens, total_tokens,
        )

    # --------------------------------------------------
    # Streaming Support
    # --------------------------------------------------

    async def _attach_streaming_hooks(self, context) -> None:
        """Register transform/result hooks so usage is captured during streaming."""
        captured: dict[str, Any] = {}

        def _capture_usage(update: Any) -> Any:
            ud = getattr(update, "usage_details", None)
            if ud:
                captured.update(ud)
            return update

        async def _on_stream_done(response: Any) -> Any:
            usage = self._extract_streaming_usage(response, captured)
            if usage:
                self._track_response_tokens(usage)
            return response

        context.stream_transform_hooks.append(_capture_usage)
        context.stream_result_hooks.append(_on_stream_done)

    def _extract_streaming_usage(self, response: Any, captured: dict[str, Any]) -> dict:
        """Extract usage from streaming response — prefer captured chunk data, fall back to response."""
        usage = dict(captured) or dict(getattr(response, "usage_details", None) or {})
        return usage

    # --------------------------------------------------
    # Token Counting
    # --------------------------------------------------

    @staticmethod
    def _message_text(m) -> str:
        """Extract plain text from a Message or dict."""
        if isinstance(m, dict):
            return str(m.get("content", ""))
        parts = []
        for item in (m.contents or []):
            parts.append(item if isinstance(item, str) else str(item))
        return " ".join(parts)

    def _count_tokens(self, messages: list) -> int:
        text = ""

        for m in messages:
            role = m.role if hasattr(m, "role") else m.get("role", "")
            content = self._message_text(m)
            text += f"{role}: {content}\n"

        return len(self.encoder.encode(text))

    # --------------------------------------------------
    # Compression Logic
    # --------------------------------------------------

    async def _compress_messages(self, messages: list) -> list:

        old_messages, recent_messages = self._split_messages(messages)

        conversation_text = "\n".join(
            f"{m.role if hasattr(m, 'role') else m.get('role', '')}: {self._message_text(m)}"
            for m in old_messages
        )

        prompt = f"""
Summarize this conversation history.

Preserve:
- user goals
- facts
- decisions
- unresolved issues
- preferences
- tool outputs

Conversation:
{conversation_text}
"""

        summary = await self._call_compression_llm(prompt)

        return [
            Message("system", [f"Conversation summary:\n{summary}"])
        ] + recent_messages

    def _split_messages(self, messages: list):
        """
        Keep recent messages.
        Preserve assistant/tool pairs.
        """

        keep = []
        i = len(messages) - 1

        while i >= 0 and len(keep) < self.keep_last_messages:
            keep.insert(0, messages[i])

            # keep assistant + tool pair together
            role_i = messages[i].role if hasattr(messages[i], "role") else messages[i].get("role", "")
            if role_i == "tool" and i > 0:
                i -= 1
                keep.insert(0, messages[i])

            i -= 1

        old = messages[: len(messages) - len(keep)]

        return old, keep

    # --------------------------------------------------
    # Compression LLM Call
    # --------------------------------------------------

    async def _call_compression_llm(self, prompt: str) -> str:
        response = await self.llm_client.get_response([Message("user", [prompt])])
        return response.text.strip()
