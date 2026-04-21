<div align="center">

# 🔑 AzureAICommunity - Agent - Token Guard Middleware

Token usage tracking and quota enforcement middleware for AI agent applications built on the **Agent Framework**.

[![PyPI Version](https://img.shields.io/pypi/v/azureaicommunity-agent-token-guard)](https://pypi.org/project/azureaicommunity-agent-token-guard/)
[![Python Versions](https://img.shields.io/pypi/pyversions/azureaicommunity-agent-token-guard)](https://pypi.org/project/azureaicommunity-agent-token-guard/)
![PyPI Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-token-guard)
[![License](https://img.shields.io/pypi/l/azureaicommunity-agent-token-guard)](https://pypi.org/project/azureaicommunity-agent-token-guard/)
[![PyPI Status](https://img.shields.io/pypi/status/azureaicommunity-agent-token-guard)](https://pypi.org/project/azureaicommunity-agent-token-guard/)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

**Track every token

[Getting Started](#-installation) · [Configuration](#️-configuration) · [Usage](#-usage) · [Contributing](#-contributing)

</div>

---

## Overview

`azureaicommunity-agent-token-guard` is a plug-and-play token tracking and quota enforcement layer for AI agent pipelines built on `agent-framework`. It captures token usage per request, accumulates it against a period quota, and blocks future requests once the limit is hit — with zero changes to your existing agent code.

---

## ✨ Features

| | Feature |
|---|---|
| 📊 | **Track token usage** — captures `input_tokens`, `output_tokens`, `total_tokens`, model, and timestamp per request |
| 🚫 | **Enforce quotas** — blocks requests before they reach the LLM once a period limit is hit |
| 🔔 | **Quota alerts** — fires a callback when the limit is exceeded (log, notify, charge) |
| 🌊 | **Streaming support** — works with both `stream=True` and regular calls |
| 📅 | **Period-flexible** — built-in `month_key`, `week_key`, `day_key` or bring your own |
| 👥 | **Per-user quotas** — pluggable `user_id_getter` for multi-tenant apps |
| 🗄️ | **Pluggable storage** — implement `QuotaStore` protocol to use Redis, Postgres, etc. |
| 🔌 | **Provider-agnostic** — works with any `agent-framework` compatible LLM client |

---

## 📦 Installation

```bash
pip install azureaicommunity-agent-token-guard
```

---

## 🚀 Quick Start

```python
import asyncio, json
from agent_framework import Agent
from agent_framework.ollama import OllamaChatClient
from token_guard_middleware import TokenGuardMiddleware
from token_guard_middleware.token_tracker import InMemoryQuotaStore, QuotaExceededError

def save_usage(record):
    print(json.dumps(record, indent=2))

def quota_alert(payload):
    print("QUOTA EXCEEDED:", json.dumps(payload, indent=2))

quota_store = InMemoryQuotaStore()

middleware = TokenGuardMiddleware(
    on_usage=save_usage,
    on_quota_exceeded=quota_alert,
    quota_store=quota_store,
    quota_tokens=50,          # intentionally low to show quota enforcement
)

async def main():
    client = OllamaChatClient(model="gemma3:4b")
    agent = Agent(client)

    # First call — succeeds and records ~60 tokens (exceeds quota of 50)
    try:
        result = await agent.run("Hello!", middleware=[middleware])
        print(result.text)
    except QuotaExceededError as e:
        print(f"Blocked: {e}")

    # Second call — quota already exceeded, quota_alert fires and call is blocked
    try:
        result = await agent.run("How are you?", middleware=[middleware])
        print(result.text)
    except QuotaExceededError as e:
        print(f"Blocked: {e}")

asyncio.run(main())
```

---

## 🧑‍💻 Usage

### Usage Record

Every call to `on_usage` receives a dict:

```json
{
  "user_id": "anonymous",
  "period_key": "2026-04",
  "model": "gemma3:4b",
  "input_tokens": 11,
  "output_tokens": 52,
  "total_tokens": 63,
  "quota_tokens": 50,
  "used_tokens_after_call": 63,
  "timestamp_utc": "2026-04-14T11:46:09.698893+00:00",
  "streaming": false
}
```

### Quota Alert Payload

When the quota is exceeded `on_quota_exceeded` receives:

```json
{
  "user_id": "anonymous",
  "period_key": "2026-04",
  "used_tokens": 63,
  "quota_tokens": 50,
  "reason": "quota_exceeded_before_call"
}
```

---

## ⚙️ Configuration

### `TokenGuardMiddleware`

| Parameter | Type | Default | Description |
|---|---|---|---|
| `on_usage` | `Callable[[dict], Any]` | **required** | Called after every successful request with the usage record |
| `quota_store` | `QuotaStore` | **required** | Storage backend for accumulated token counts |
| `quota_tokens` | `int` | **required** | Max tokens allowed per period |
| `on_quota_exceeded` | `Callable[[dict], Any]` | `None` | Called when quota is hit (before raising) |
| `user_id_getter` | `Callable[[ChatContext], str]` | `default_user_id_getter` | Extracts user/tenant ID from context |
| `period_key_fn` | `Callable[[], str]` | `month_key` | Returns the current billing period key |

### Period key functions

```python
from token_guard_middleware.token_tracker import month_key, week_key, day_key

middleware = TokenGuardMiddleware(..., period_key_fn=month_key)   # Monthly (default)
middleware = TokenGuardMiddleware(..., period_key_fn=day_key)     # Daily
middleware = TokenGuardMiddleware(..., period_key_fn=week_key)    # Weekly

# Custom — e.g. per-user-per-day
middleware = TokenGuardMiddleware(
    ...,
    period_key_fn=lambda: f"{get_current_user_id()}-{day_key()}",
)
```

### Per-user quotas

```python
def get_user_id(context):
    return context.metadata.get("user_id", "anonymous")

middleware = TokenGuardMiddleware(
    ...,
    user_id_getter=get_user_id,
)
```

### Custom Storage Backend

Implement the `QuotaStore` protocol to persist usage in Redis, Postgres, or any other store:

```python
from token_guard_middleware.token_tracker import QuotaStore

class RedisQuotaStore:
    def get_usage(self, user_id: str, period_key: str) -> int:
        return int(redis.get(f"{user_id}:{period_key}") or 0)

    def add_usage(self, user_id: str, period_key: str, tokens: int) -> None:
        redis.incrby(f"{user_id}:{period_key}", tokens)

middleware = TokenGuardMiddleware(
    ...,
    quota_store=RedisQuotaStore(),
)
```

---

## ⚙️ How It Works

```
1. Intercept  →  middleware captures the outgoing agent request
2. Check      →  quota store is queried for current period usage
3. Block      →  if quota exceeded, raises QuotaExceededError before calling LLM
4. Forward    →  request proceeds to the LLM provider
5. Track      →  response token counts are extracted and written to quota store
6. Notify     →  on_usage callback fires with the full usage record
```

**Provider Compatibility:**

Works with any LLM client that implements the `agent-framework` `ChatClient` interface.

---

## 🤝 Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to change before submitting a pull request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 👤 Author

Built and maintained by **Vinoth Rajendran**.

- 🐙 GitHub: [github.com/rvinothrajendran](https://github.com/rvinothrajendran) — _follow for more projects!_
- 📺 YouTube: [youtube.com/@VinothRajendran](https://www.youtube.com/@VinothRajendran) — _subscribe for tutorials and demos!_
- 💼 LinkedIn: [linkedin.com/in/rvinothrajendran](https://www.linkedin.com/in/rvinothrajendran/) — _let's connect!_

---

## 📄 License

MIT — see [LICENSE](LICENSE) for details.
