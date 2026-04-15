
import asyncio
import json
import logging
from typing import Any

logging.basicConfig(level=logging.INFO, format="%(levelname)s - %(name)s - %(message)s")

from agent_framework import Agent
from agent_framework.ollama import OllamaChatClient

from token_guard_middleware import TokenGuardMiddleware
from token_guard_middleware.token_tracker import InMemoryQuotaStore, QuotaExceededError


def save_usage(record: dict[str, Any]) -> None:
    print("\n--- Usage Record ---")
    print(json.dumps(record, indent=2, default=str))


def quota_alert(payload: dict[str, Any]) -> None:
    print("\n--- QUOTA EXCEEDED ---")
    print(json.dumps(payload, indent=2, default=str))


async def main():
    client = OllamaChatClient(model="gemma3:4b")
    agent = Agent(client)

    # --- Example 1: InMemoryQuotaStore ---
    # quota_tokens=50 is intentionally low to trigger quota_alert on the second call
    memory_store = InMemoryQuotaStore()
    memory_middleware = TokenGuardMiddleware(
        on_usage=save_usage,
        on_quota_exceeded=quota_alert,
        quota_store=memory_store,
        quota_tokens=50,
    )
    try:
        result = await agent.run("Hello!", middleware=[memory_middleware])
        print("\nResponse:", result.text)
    except QuotaExceededError as e:
        print(f"Blocked: {e}")

    # Second call — quota already exceeded, quota_alert fires and QuotaExceededError is raised
    try:
        result = await agent.run("How are you?", middleware=[memory_middleware])
        print("\nResponse:", result.text)
    except QuotaExceededError as e:
        print(f"\nBlocked: {e}")


asyncio.run(main())
