"""
agent_context_compression.py
Demonstrates ContextCompressionMiddleware across three scenarios:

  1. Auto-compression  — long conversation that triggers automatic history compression
  2. Callback: allow   — on_threshold_reached returns True  → compression proceeds
  3. Callback: block   — on_threshold_reached returns False → TokenThresholdReachedError raised
  4. Streaming         — compression + usage tracking with stream=True
"""

import asyncio
import logging
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "Middleware", "ContextCompression"))

from agent_framework.ollama import OllamaChatClient

from context_compression import ContextCompressionMiddleware, TokenThresholdReachedError  # type: ignore[import-untyped]

logging.basicConfig(level=logging.INFO, format="%(levelname)s - %(name)s - %(message)s")

# ---------------------------------------------------------------------------
# Shared LLM client (used both as the agent and as the summarizer)
# ---------------------------------------------------------------------------
llm_client = OllamaChatClient(model="gemma3:4b")


def print_usage(label: str, middleware: ContextCompressionMiddleware) -> None:
    u = middleware.last_usage
    if u:
        print(
            f"  [{label}] token usage — "
            f"input={u['input_token_count']}, "
            f"output={u['output_token_count']}, "
            f"total={u['total_token_count']}"
        )


# ---------------------------------------------------------------------------
# Scenario 1: Auto-compression
# A low max_tokens (300) and keep_last_messages=2 force compression after a
# few turns so you can see the middleware summarise old history automatically.
# ---------------------------------------------------------------------------
async def demo_auto_compression() -> None:
    print("\n" + "=" * 60)
    print("Scenario 1: Auto-compression")
    print("=" * 60)

    summarizer = ContextCompressionMiddleware(
        llm_client=llm_client,
        max_tokens=300,        # intentionally low to trigger compression quickly
        trigger_ratio=0.80,    # fires at 240 tokens
        keep_last_messages=2,  # keep only the 2 most recent messages verbatim
        logger=logging.getLogger("AutoCompression"),
    )

    agent = llm_client.as_agent(
        name="AutoAgent",
        instructions="You are a helpful assistant. Keep answers short.",
        middleware=[summarizer],
    )
    session = agent.create_session()

    conversation = [
        "Hi, my name is Vinoth and I live in Chennai.",
        "I have been working with Python for 10 years.",
        "My favourite framework is agent-framework.",
        "I also enjoy building AI-powered tools.",
        "What city do I live in?",         # should still be answered from summary
        "What programming language do I use?",
    ]

    for message in conversation:
        response = await agent.run(message, session=session)
        print(f"  User : {message}")
        print(f"  Agent: {response.text.strip()}")
        print_usage("auto", summarizer)
        print()


# ---------------------------------------------------------------------------
# Scenario 2: on_threshold_reached — allow compression
# The callback logs the event and returns True so compression proceeds.
# ---------------------------------------------------------------------------
async def demo_callback_allow() -> None:
    print("\n" + "=" * 60)
    print("Scenario 2: on_threshold_reached — allow compression")
    print("=" * 60)

    def allow_compression(info: dict) -> bool:
        print(
            f"  [callback] threshold hit — "
            f"tokens_used={info['tokens_used']}, "
            f"max={info['max_tokens']}, "
            f"trigger={info['trigger_tokens']} → allowing compression"
        )
        return True   # proceed with compression

    summarizer = ContextCompressionMiddleware(
        llm_client=llm_client,
        max_tokens=300,
        trigger_ratio=0.80,
        keep_last_messages=2,
        on_threshold_reached=allow_compression,
        logger=logging.getLogger("CallbackAllow"),
    )

    agent = llm_client.as_agent(
        name="AllowAgent",
        instructions="You are a helpful assistant. Keep answers short.",
        middleware=[summarizer],
    )
    session = agent.create_session()

    messages = [
        "My name is Vinoth.",
        "I work on AI middleware.",
        "I prefer concise documentation.",
        "What is my name?",
    ]

    for message in messages:
        response = await agent.run(message, session=session)
        print(f"  User : {message}")
        print(f"  Agent: {response.text.strip()}")
        print_usage("allow", summarizer)
        print()


# ---------------------------------------------------------------------------
# Scenario 3: on_threshold_reached — block the request
# The callback returns False once the token count is too high, which causes
# TokenThresholdReachedError to be raised instead of compressing.
# ---------------------------------------------------------------------------
async def demo_callback_block() -> None:
    print("\n" + "=" * 60)
    print("Scenario 3: on_threshold_reached — block the request")
    print("=" * 60)

    def block_when_too_large(info: dict) -> bool:
        print(
            f"  [callback] threshold hit — "
            f"tokens_used={info['tokens_used']} → BLOCKING request"
        )
        return False   # block — raises TokenThresholdReachedError

    summarizer = ContextCompressionMiddleware(
        llm_client=llm_client,
        max_tokens=300,
        trigger_ratio=0.80,
        keep_last_messages=2,
        on_threshold_reached=block_when_too_large,
        logger=logging.getLogger("CallbackBlock"),
    )

    agent = llm_client.as_agent(
        name="BlockAgent",
        instructions="You are a helpful assistant. Keep answers short.",
        middleware=[summarizer],
    )
    session = agent.create_session()

    messages = [
        "My name is Vinoth.",
        "I work on AI middleware.",
        "I prefer concise documentation.",
        "What is my name?",   # this turn should trigger the block
    ]

    for message in messages:
        try:
            response = await agent.run(message, session=session)
            print(f"  User : {message}")
            print(f"  Agent: {response.text.strip()}")
        except TokenThresholdReachedError as e:
            print(f"  User : {message}")
            print(f"  BLOCKED: {e}")
        print()


# ---------------------------------------------------------------------------
# Scenario 4: Streaming with compression + usage tracking
# ---------------------------------------------------------------------------
async def demo_streaming() -> None:
    print("\n" + "=" * 60)
    print("Scenario 4: Streaming with auto-compression")
    print("=" * 60)

    summarizer = ContextCompressionMiddleware(
        llm_client=llm_client,
        max_tokens=300,
        trigger_ratio=0.80,
        keep_last_messages=2,
        logger=logging.getLogger("Streaming"),
    )

    agent = llm_client.as_agent(
        name="StreamAgent",
        instructions="You are a helpful assistant. Keep answers short.",
        middleware=[summarizer],
    )
    session = agent.create_session()

    messages = [
        "My name is Vinoth.",
        "I work on AI middleware.",
        "I prefer concise documentation.",
        "What is my name?",
    ]

    for message in messages:
        print(f"  User : {message}")
        print(  "  Agent: ", end="")
        stream = agent.run(message, session=session, stream=True)
        async for update in stream:
            chunk = getattr(update, "text", None)
            if chunk:
                print(chunk, end="", flush=True)
        print()
        await stream.get_final_response()
        print_usage("stream", summarizer)
        print()


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------
async def main() -> None:
    await demo_auto_compression()
    await demo_callback_allow()
    await demo_callback_block()
    await demo_streaming()


asyncio.run(main())
