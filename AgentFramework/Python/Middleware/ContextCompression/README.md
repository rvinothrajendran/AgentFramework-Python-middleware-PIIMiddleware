<div align="center">

# 🗜️ AzureAICommunity - Agent - Context Compression Middleware

Automatic conversation history compression middleware for AI agent applications built on the **Agent Framework**.

[![PyPI Version](https://img.shields.io/pypi/v/agentaicommunity-agent-context-compression)](https://pypi.org/project/agentaicommunity-agent-context-compression/)
[![Python Versions](https://img.shields.io/pypi/pyversions/agentaicommunity-agent-context-compression)](https://pypi.org/project/agentaicommunity-agent-context-compression/)
![PyPI Downloads](https://static.pepy.tech/badge/agentaicommunity-agent-context-compression)
[![License](https://img.shields.io/pypi/l/agentaicommunity-agent-context-compression)](https://pypi.org/project/agentaicommunity-agent-context-compression/)
[![PyPI Status](https://img.shields.io/pypi/status/agentaicommunity-agent-context-compression)](https://pypi.org/project/agentaicommunity-agent-context-compression/)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

**Keep long multi-turn

[Getting Started](#-installation) · [Configuration](#️-configuration) · [Usage](#-usage) · [Contributing](#-contributing)

</div>

---

## Overview

`agentaicommunity-agent-context-compression` is a plug-and-play context management layer for AI agent pipelines built on `agent-framework`. It counts tokens in the conversation history before each LLM call, and when the count approaches your configured limit it automatically summarises the older messages — keeping the session alive without hitting context-window errors.

---

## ✨ Features

| | Feature |
|---|---|
| 🗜️ | **Automatic compression** — fires transparently when the token threshold is hit |
| ⚙️ | **Configurable trigger** — set `max_tokens` and `trigger_ratio` to match your model's context window |
| 🔒 | **Keep recent messages** — `keep_last_messages` preserves the tail of the conversation verbatim |
| 🔧 | **Tool-call aware** — keeps `assistant` + `tool` message pairs together during split |
| 🔔 | **Block or allow** — `on_threshold_reached` callback lets you log, alert, or stop the request |
| 📝 | **Structured logging** — pass your own `logging.Logger`; no `print()` calls |
| 📊 | **Token usage tracking** — `last_usage` on the middleware instance after each call (both modes) |
| 🌊 | **Streaming support** — works transparently with `stream=True`; usage captured via framework hooks |
| 🔌 | **Provider-agnostic** — works with any `agent-framework` LLM client (Ollama, OpenAI, Azure, etc.) |

---

## 📦 Installation

```bash
pip install agentaicommunity-agent-context-compression
```

Or install from source:

```bash
cd ContextCompression
pip install -e .
```

---

## 🚀 Quick Start

### Non-streaming

```python
import asyncio
import logging
from agent_framework.ollama import OllamaChatClient
from context_compression import ContextCompressionMiddleware, TokenThresholdReachedError

logging.basicConfig(level=logging.INFO)

compressor = ContextCompressionMiddleware(
    llm_client=OllamaChatClient(model="gemma3:4b"),  # LLM used to write the summary
    max_tokens=8000,        # compress when history approaches this size
    trigger_ratio=0.80,     # fire at 80% = 6400 tokens
    keep_last_messages=8,   # always keep the 8 most recent messages verbatim
    logger=logging.getLogger("ContextCompression"),
)

agent = OllamaChatClient(model="gemma3:4b").as_agent(
    name="MyAgent",
    instructions="You are a helpful assistant.",
    middleware=[compressor],
)
session = agent.create_session()

async def main():
    for message in ["Hi, my name is Vinoth.", "I work in Python.", "What is my name?"]:
        response = await agent.run(message, session=session)
        print(response.text)

asyncio.run(main())
```

### Streaming

```python
async def main():
    messages = ["Hi, my name is Vinoth.", "I work in Python.", "What is my name?"]
    for message in messages:
        stream = agent.run(message, session=session, stream=True)
        async for update in stream:
            chunk = getattr(update, "text", None)
            if chunk:
                print(chunk, end="", flush=True)
        print()
        await stream.get_final_response()  # finalizes stream and populates last_usage

asyncio.run(main())
```

---

## 🧑‍💻 Usage

### Threshold Callback Payload

Every call to `on_threshold_reached` receives a dict:

```python
{
    "tokens_used":    87,   # current history token count
    "max_tokens":    100,   # your configured max
    "trigger_tokens": 75,   # the threshold that was crossed
}
```

Return `True` → compression proceeds normally.  
Return `False` → request is blocked and `TokenThresholdReachedError` is raised.

### Token Usage After Each Call

`last_usage` is populated after every call — both streaming and non-streaming:

```python
# Non-streaming
response = await agent.run("Hello", session=session)

# Streaming
stream = agent.run("Hello", session=session, stream=True)
async for update in stream:
    pass
await stream.get_final_response()

# Either way, last_usage is populated:
u = compressor.last_usage
print(u["input_token_count"])   # tokens sent to LLM
print(u["output_token_count"])  # tokens in the response
print(u["total_token_count"])   # input + output
```

### Handling `TokenThresholdReachedError`

```python
from context_compression import ContextCompressionMiddleware, TokenThresholdReachedError

try:
    response = await agent.run(message, session=session)
except TokenThresholdReachedError as e:
    print(f"Blocked: {e}")
    # handle gracefully — notify user, end session, etc.
```

---

## ⚙️ Configuration

### `ContextCompressionMiddleware`

| Parameter | Type | Default | Description |
|---|---|---|---|
| `llm_client` | any LLM client | **required** | Client used to generate the summary (can be a smaller/cheaper model) |
| `max_tokens` | `int` | `8000` | History size limit (tiktoken count) |
| `trigger_ratio` | `float` | `0.80` | Compression fires at `max_tokens × trigger_ratio` |
| `keep_last_messages` | `int` | `8` | Number of recent messages to keep verbatim after compression |
| `model_encoding` | `str` | `"cl100k_base"` | tiktoken encoding for token counting |
| `on_threshold_reached` | `Callable[[dict], bool]` | `None` | Callback fired at threshold. Return `True` to compress, `False` to block |
| `logger` | `logging.Logger` | `None` | Your logger. Falls back to `logging.getLogger(__name__)` |

### Blocking runaway sessions

```python
def my_callback(info: dict) -> bool:
    if info["tokens_used"] > 500_000:
        return False   # block — raises TokenThresholdReachedError
    return True        # allow compression

middleware = ContextCompressionMiddleware(
    ...,
    on_threshold_reached=my_callback,
)
```

---

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
