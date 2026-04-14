import asyncio
import json
import logging
import os
from typing import Any

logging.basicConfig(level=logging.INFO, format="%(levelname)s - %(name)s - %(message)s")

from agent_framework import Agent
from agent_framework.ollama import OllamaChatClient

from token_guard_middleware import TokenGuardMiddleware
from token_guard_middleware.token_tracker import QuotaExceededError


# ---------------------------------------------------------------------------
# Custom Storage Backend — implement QuotaStore to persist usage anywhere
# (Redis, Postgres, SQLite, etc.). This example uses a plain JSON file.
# ---------------------------------------------------------------------------
class JsonFileQuotaStore:
    def __init__(self, path: str = "quota_usage.json") -> None:
        self._path = path
        self._load()

    def _load(self) -> None:
        if os.path.exists(self._path):
            with open(self._path) as f:
                raw = json.load(f)
            self._totals: dict[str, int] = {k: int(v) for k, v in raw.items()}
        else:
            self._totals = {}

    def _save(self) -> None:
        with open(self._path, "w") as f:
            json.dump(self._totals, f, indent=2)

    def get_usage(self, user_id: str, period_key: str) -> int:
        return self._totals.get(f"{user_id}:{period_key}", 0)

    def add_usage(self, user_id: str, period_key: str, tokens: int) -> None:
        key = f"{user_id}:{period_key}"
        self._totals[key] = self._totals.get(key, 0) + int(tokens)
        self._save()


USAGE_LOG = os.path.join(os.path.dirname(os.path.abspath(__file__)), "usage_log.json")


def save_usage(record: dict[str, Any]) -> None:
    print("\n--- Usage Record ---")
    print(json.dumps(record, indent=2, default=str))

    # Append the full record to usage_log.json
    records: list = []
    if os.path.exists(USAGE_LOG):
        with open(USAGE_LOG) as f:
            records = json.load(f)
    records.append(record)
    with open(USAGE_LOG, "w") as f:
        json.dump(records, f, indent=2, default=str)


def quota_alert(payload: dict[str, Any]) -> None:
    print("\n--- QUOTA EXCEEDED ---")
    print(json.dumps(payload, indent=2, default=str))


async def main():
    client = OllamaChatClient(model="gemma3:4b")
    agent = Agent(client)

    file_store = JsonFileQuotaStore("quota_usage.json")
    middleware = TokenGuardMiddleware(
        on_usage=save_usage,
        on_quota_exceeded=quota_alert,
        quota_store=file_store,
        quota_tokens=100_000,
    )

    try:
        result = await agent.run("What is 2 + 2?", middleware=[middleware])
        print("\nResponse:", result.text)
    except QuotaExceededError as e:
        print(f"Blocked: {e}")


asyncio.run(main())
