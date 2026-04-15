"""Unit tests for TokenGuardMiddleware.

Covers:
- Quota blocking (QuotaExceededError raised when quota exhausted)
- on_quota_exceeded callback invocation
- Non-streaming token accounting
- Streaming token accounting (via transform + result hooks)
"""
from __future__ import annotations

import pytest
from agent_framework import ChatContext

from token_guard_middleware import InMemoryQuotaStore, QuotaExceededError, TokenGuardMiddleware

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

FIXED_PERIOD = "2024-01"


def _fixed_period() -> str:
    return FIXED_PERIOD


class _MockResult:
    """Minimal stand-in for a ChatResponse with usage_details."""

    def __init__(self, usage_details: dict | None = None, model: str | None = None) -> None:
        self.usage_details = usage_details or {}
        self.model = model


def _make_context(
    *,
    stream: bool = False,
    usage_details: dict | None = None,
    user_id: str = "user1",
) -> ChatContext:
    ctx = ChatContext(client=None, messages=[], options=None, stream=stream)
    ctx.metadata["user_id"] = user_id
    if usage_details is not None:
        ctx.result = _MockResult(usage_details)
    return ctx


async def _noop() -> None:
    pass


def _make_middleware(
    *,
    quota_tokens: int = 10_000,
    on_quota_exceeded=None,
    store: InMemoryQuotaStore | None = None,
) -> tuple[TokenGuardMiddleware, InMemoryQuotaStore, list]:
    if store is None:
        store = InMemoryQuotaStore()
    recorded: list = []
    mw = TokenGuardMiddleware(
        on_usage=lambda r: recorded.append(r),
        quota_store=store,
        quota_tokens=quota_tokens,
        period_key_fn=_fixed_period,
        on_quota_exceeded=on_quota_exceeded,
    )
    return mw, store, recorded


# ---------------------------------------------------------------------------
# Quota blocking
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_quota_blocked_when_limit_reached() -> None:
    mw, store, _ = _make_middleware(quota_tokens=1000)
    store.add_usage("user1", FIXED_PERIOD, 1000)
    ctx = _make_context()

    with pytest.raises(QuotaExceededError):
        await mw.process(ctx, _noop)


@pytest.mark.asyncio
async def test_quota_blocked_when_limit_exceeded() -> None:
    mw, store, _ = _make_middleware(quota_tokens=1000)
    store.add_usage("user1", FIXED_PERIOD, 1500)
    ctx = _make_context()

    with pytest.raises(QuotaExceededError):
        await mw.process(ctx, _noop)


@pytest.mark.asyncio
async def test_quota_not_blocked_below_limit() -> None:
    mw, store, recorded = _make_middleware(quota_tokens=1000)
    store.add_usage("user1", FIXED_PERIOD, 500)
    ctx = _make_context(
        usage_details={"total_token_count": 100, "input_token_count": 40, "output_token_count": 60}
    )

    await mw.process(ctx, _noop)  # must not raise

    assert len(recorded) == 1
    assert recorded[0]["total_tokens"] == 100


# ---------------------------------------------------------------------------
# on_quota_exceeded callback
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_on_quota_exceeded_callback_invoked() -> None:
    exceeded: list = []
    mw, store, _ = _make_middleware(
        quota_tokens=1000,
        on_quota_exceeded=lambda p: exceeded.append(p),
    )
    store.add_usage("user1", FIXED_PERIOD, 1000)
    ctx = _make_context()

    with pytest.raises(QuotaExceededError):
        await mw.process(ctx, _noop)

    assert len(exceeded) == 1
    assert exceeded[0]["user_id"] == "user1"
    assert exceeded[0]["used_tokens"] == 1000
    assert exceeded[0]["quota_tokens"] == 1000
    assert exceeded[0]["reason"] == "quota_exceeded_before_call"


@pytest.mark.asyncio
async def test_on_quota_exceeded_not_called_under_quota() -> None:
    exceeded: list = []
    mw, store, _ = _make_middleware(
        quota_tokens=1000,
        on_quota_exceeded=lambda p: exceeded.append(p),
    )
    ctx = _make_context(usage_details={"total_token_count": 100})

    await mw.process(ctx, _noop)

    assert exceeded == []


# ---------------------------------------------------------------------------
# Non-streaming accounting
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_non_streaming_usage_recorded() -> None:
    mw, store, recorded = _make_middleware(quota_tokens=10_000)
    ctx = _make_context(
        usage_details={
            "total_token_count": 300,
            "input_token_count": 100,
            "output_token_count": 200,
        }
    )

    await mw.process(ctx, _noop)

    assert store.get_usage("user1", FIXED_PERIOD) == 300
    assert len(recorded) == 1
    rec = recorded[0]
    assert rec["total_tokens"] == 300
    assert rec["input_tokens"] == 100
    assert rec["output_tokens"] == 200
    assert rec["streaming"] is False
    assert rec["user_id"] == "user1"
    assert rec["period_key"] == FIXED_PERIOD


@pytest.mark.asyncio
async def test_non_streaming_total_derived_from_input_plus_output() -> None:
    """total_token_count absent: total should be computed as input + output."""
    mw, store, recorded = _make_middleware(quota_tokens=10_000)
    ctx = _make_context(
        usage_details={"input_token_count": 80, "output_token_count": 120}
    )

    await mw.process(ctx, _noop)

    assert store.get_usage("user1", FIXED_PERIOD) == 200
    assert recorded[0]["total_tokens"] == 200


@pytest.mark.asyncio
async def test_non_streaming_no_usage_when_result_missing() -> None:
    """No result → no on_usage callback and no stored tokens."""
    mw, store, recorded = _make_middleware(quota_tokens=10_000)
    ctx = _make_context()  # no usage_details

    await mw.process(ctx, _noop)

    assert store.get_usage("user1", FIXED_PERIOD) == 0
    assert recorded == []


# ---------------------------------------------------------------------------
# Streaming accounting
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_streaming_usage_recorded_via_result_hook() -> None:
    mw, store, recorded = _make_middleware(quota_tokens=10_000)
    ctx = _make_context(stream=True)

    class _StreamResponse:
        usage_details = {
            "total_token_count": 250,
            "input_token_count": 100,
            "output_token_count": 150,
        }
        model = "test-model"

    await mw.process(ctx, _noop)

    assert len(ctx.stream_result_hooks) == 1
    await ctx.stream_result_hooks[0](_StreamResponse())

    assert store.get_usage("user1", FIXED_PERIOD) == 250
    assert len(recorded) == 1
    rec = recorded[0]
    assert rec["total_tokens"] == 250
    assert rec["streaming"] is True
    assert rec["user_id"] == "user1"


@pytest.mark.asyncio
async def test_streaming_usage_captured_from_transform_hook() -> None:
    """Usage surfaced by a mid-stream update (captured via transform hook) is used
    when the final response carries no usage_details."""
    mw, store, recorded = _make_middleware(quota_tokens=10_000)
    ctx = _make_context(stream=True)

    class _StreamUpdate:
        usage_details = {
            "total_token_count": 180,
            "input_token_count": 60,
            "output_token_count": 120,
        }

    class _StreamResponse:
        usage_details = None  # provider doesn't repeat usage on the final response
        model = None

    await mw.process(ctx, _noop)

    # Simulate a streaming update that carries usage metadata
    assert len(ctx.stream_transform_hooks) == 1
    ctx.stream_transform_hooks[0](_StreamUpdate())

    # Simulate stream completion
    await ctx.stream_result_hooks[0](_StreamResponse())

    assert store.get_usage("user1", FIXED_PERIOD) == 180
    assert recorded[0]["total_tokens"] == 180
    assert recorded[0]["streaming"] is True


@pytest.mark.asyncio
async def test_streaming_quota_blocked() -> None:
    mw, store, _ = _make_middleware(quota_tokens=500)
    store.add_usage("user1", FIXED_PERIOD, 500)
    ctx = _make_context(stream=True)

    with pytest.raises(QuotaExceededError):
        await mw.process(ctx, _noop)


# ---------------------------------------------------------------------------
# Multi-user isolation
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_different_users_have_independent_quotas() -> None:
    store = InMemoryQuotaStore()
    recorded: list = []
    mw = TokenGuardMiddleware(
        on_usage=lambda r: recorded.append(r),
        quota_store=store,
        quota_tokens=1000,
        period_key_fn=_fixed_period,
    )
    store.add_usage("alice", FIXED_PERIOD, 999)

    ctx_alice = _make_context(
        user_id="alice",
        usage_details={"total_token_count": 1},
    )
    await mw.process(ctx_alice, _noop)

    ctx_bob = _make_context(
        user_id="bob",
        usage_details={"total_token_count": 500},
    )
    await mw.process(ctx_bob, _noop)

    assert store.get_usage("alice", FIXED_PERIOD) == 1000
    assert store.get_usage("bob", FIXED_PERIOD) == 500
