from __future__ import annotations

from typing import Any, Awaitable, Callable, Optional

from agent_framework import ChatContext, ChatMiddleware

from token_guard_middleware.token_tracker import (
    QuotaExceededError,
    QuotaStore,
    UsageCallback,
    build_usage_record,
    default_user_id_getter,
    extract_usage_details,
    maybe_await,
    month_key,
)


class TokenGuardMiddleware(ChatMiddleware):
    def __init__(
        self,
        *,
        on_usage: UsageCallback,
        quota_store: QuotaStore,
        quota_tokens: int,
        user_id_getter: Callable[[ChatContext], str] = default_user_id_getter,
        period_key_fn: Callable[[], str] = month_key,
        on_quota_exceeded: Optional[UsageCallback] = None,
    ) -> None:
        self.on_usage = on_usage
        self.on_quota_exceeded = on_quota_exceeded
        self.quota_store = quota_store
        self.quota_tokens = int(quota_tokens)
        self.user_id_getter = user_id_getter
        self.period_key_fn = period_key_fn

    async def process(self, context: ChatContext, next: Callable[[], Awaitable[None]]) -> None:
        user_id = self.user_id_getter(context)
        period_key = self.period_key_fn()

        used_so_far = self.quota_store.get_usage(user_id, period_key)
        if used_so_far >= self.quota_tokens:
            payload = {
                "user_id": user_id,
                "period_key": period_key,
                "used_tokens": used_so_far,
                "quota_tokens": self.quota_tokens,
                "reason": "quota_exceeded_before_call",
            }
            if self.on_quota_exceeded:
                await maybe_await(self.on_quota_exceeded(payload))
            raise QuotaExceededError(
                f"Quota exceeded for user={user_id}. "
                f"Used={used_so_far}, quota={self.quota_tokens}"
            )

        if context.stream:
            captured: dict[str, Any] = {}

            def _capture_usage(update: Any) -> Any:
                ud = getattr(update, "usage_details", None)
                if ud:
                    captured.update(ud)
                return update

            async def _on_stream_done(response: Any) -> Any:
                usage = self._extract_streaming_usage(response, captured)
                if usage:
                    total = int(usage.get("total_token_count") or 0)
                    inp = usage.get("input_token_count")
                    out = usage.get("output_token_count")
                    if not total:
                        total = int(inp or 0) + int(out or 0)
                    self.quota_store.add_usage(user_id, period_key, total)
                    record = build_usage_record(
                        user_id=user_id,
                        period_key=period_key,
                        model=getattr(response, "model", None),
                        input_tokens=inp,
                        output_tokens=out,
                        total_tokens=total,
                        quota_tokens=self.quota_tokens,
                        used_tokens_after_call=self.quota_store.get_usage(user_id, period_key),
                        streaming=True,
                    )
                    await maybe_await(self.on_usage(record))
                return response

            context.stream_transform_hooks.append(_capture_usage)
            context.stream_result_hooks.append(_on_stream_done)
            await next()
            return

        await next()

        usage = extract_usage_details(context)
        if not usage:
            return

        total_tokens = int(usage.get("total_token_count") or 0)
        prompt_tokens = usage.get("input_token_count")
        completion_tokens = usage.get("output_token_count")
        if not total_tokens:
            total_tokens = int(prompt_tokens or 0) + int(completion_tokens or 0)

        self.quota_store.add_usage(user_id, period_key, total_tokens)

        record = build_usage_record(
            user_id=user_id,
            period_key=period_key,
            model=getattr(getattr(context, "result", None), "model", None),
            input_tokens=prompt_tokens,
            output_tokens=completion_tokens,
            total_tokens=total_tokens,
            quota_tokens=self.quota_tokens,
            used_tokens_after_call=self.quota_store.get_usage(user_id, period_key),
            streaming=False,
        )
        await maybe_await(self.on_usage(record))

    def _extract_streaming_usage(self, response: Any, captured: dict[str, Any]) -> dict[str, Any]:
        """Returns a usage dict from the final streaming response.

        Override in a subclass to support providers whose streaming responses
        don't surface usage_details on the final ChatResponse.
        """
        return dict(captured) or dict(getattr(response, "usage_details", None) or {})
