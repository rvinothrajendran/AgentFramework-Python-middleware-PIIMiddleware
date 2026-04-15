from __future__ import annotations

import inspect
from datetime import datetime, timezone
from typing import Any, Callable, Protocol

from agent_framework import ChatContext


UsageCallback = Callable[[dict[str, Any]], Any]


class QuotaExceededError(RuntimeError):
    pass


class QuotaStore(Protocol):
    def get_usage(self, user_id: str, period_key: str) -> int: ...
    def add_usage(self, user_id: str, period_key: str, tokens: int) -> None: ...


class InMemoryQuotaStore:
    """In-process quota store backed by a plain dict.

    Safe for use in a single asyncio event loop (no awaits between read and
    write, so no concurrent coroutine can interleave). Not safe for use across
    multiple OS threads or multiple processes — use a shared backend
    (Redis, Postgres, etc.) via the QuotaStore protocol for those cases.
    """

    def __init__(self) -> None:
        self._totals: dict[tuple[str, str], int] = {}

    def get_usage(self, user_id: str, period_key: str) -> int:
        return self._totals.get((user_id, period_key), 0)

    def add_usage(self, user_id: str, period_key: str, tokens: int) -> None:
        key = (user_id, period_key)
        self._totals[key] = self._totals.get(key, 0) + int(tokens)


def month_key() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m")


def day_key() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%d")


def week_key() -> str:
    iso_year, iso_week, _ = datetime.now(timezone.utc).isocalendar()
    return f"{iso_year}-W{iso_week:02d}"


def utc_now_key() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


async def maybe_await(value: Any) -> Any:
    if inspect.isawaitable(value):
        return await value
    return value


def default_user_id_getter(context: ChatContext) -> str:
    for candidate in (
        getattr(context, "user_id", None),
        getattr(getattr(context, "metadata", None), "get", lambda *_: None)("user_id"),
    ):
        if candidate:
            return str(candidate)
    return "anonymous"


def build_usage_record(
    *,
    user_id: str,
    period_key: str,
    model: str | None,
    input_tokens: int | None,
    output_tokens: int | None,
    total_tokens: int,
    quota_tokens: int,
    used_tokens_after_call: int,
    streaming: bool,
) -> dict[str, Any]:
    return {
        "user_id": user_id,
        "period_key": period_key,
        "model": model,
        "input_tokens": input_tokens,
        "output_tokens": output_tokens,
        "total_tokens": total_tokens,
        "quota_tokens": quota_tokens,
        "used_tokens_after_call": used_tokens_after_call,
        "timestamp_utc": datetime.now(timezone.utc).isoformat(),
        "streaming": streaming,
    }


def extract_usage_details(context: ChatContext) -> dict[str, Any] | None:
    result = getattr(context, "result", None)
    if not result:
        return None
    usage = getattr(result, "usage_details", None)
    if not usage:
        return None
    return dict(usage)
