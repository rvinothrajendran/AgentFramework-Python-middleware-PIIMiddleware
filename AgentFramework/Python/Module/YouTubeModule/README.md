<div align="center">

# 📺 AzureAICommunity - Agent - YouTube Search

YouTube video search tools for AI agent applications built on the **Agent Framework**.

[![PyPI Version](https://img.shields.io/pypi/v/azureaicommunity-agent-youtube-search)](https://pypi.org/project/azureaicommunity-agent-youtube-search/)
[![Python Versions](https://img.shields.io/pypi/pyversions/azureaicommunity-agent-youtube-search)](https://pypi.org/project/azureaicommunity-agent-youtube-search/)
![PyPI Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-youtube-search)
[![License](https://img.shields.io/pypi/l/azureaicommunity-agent-youtube-search)](https://pypi.org/project/azureaicommunity-agent-youtube-search/)
[![PyPI Status](https://img.shields.io/pypi/status/azureaicommunity-agent-youtube-search)](https://pypi.org/project/azureaicommunity-agent-youtube-search/)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

[Getting Started](#-installation) · [Configuration](#️-configuration) · [Usage](#-usage) · [Contributing](#-contributing)

</div>

---

## Overview

`azureaicommunity-agent-youtube-search` provides a `YouTubeTools` class that registers a `search_youtube_videos` `@tool` function directly into any `agent-framework` agent. The agent can search YouTube using natural language — with optional channel scoping, result count, and paged offset support.

---

## ✨ Features

| | Feature |
|---|---|
| 🔍 | **Natural language search** — find YouTube videos from any query string |
| 📺 | **Rich results** — title, description, and watch URL for every result |
| 📡 | **YouTube Data API v3** — fast, accurate, production-grade results |
| 📋 | **Channel scoping** — restrict results to a specific YouTube channel |
| 🔢 | **Paged results** — built-in `count` and `offset` for paginated search |
| 🔌 | **Agent-ready** — `@tool`-decorated function drops into any `agent-framework` agent |
| 📦 | **Provider-agnostic** — works with Ollama, Azure OpenAI, or any compatible client |

---

## 📦 Installation

```bash
pip install azureaicommunity-agent-youtube-search
```

---

## 🚀 Quick Start

```python
import asyncio
import os
from agent_framework.ollama import OllamaChatClient
from youtube_search_module import YouTubeConfig, YouTubeTools

config = YouTubeConfig(
    api_key=os.environ["YOUTUBE_API_KEY"],
    default_count=5,
)
tools = YouTubeTools.create(config)

agent = OllamaChatClient(model="llama3.2").as_agent(
    name="YouTubeAgent",
    instructions="You are a helpful YouTube search assistant. Use search_youtube_videos to find videos.",
    tools=tools,
)

async def main():
    session = agent.create_session()
    response = await agent.run("Find Python tutorial videos for beginners.", session=session)
    print(response.text)

asyncio.run(main())
```

---

## 🧑‍💻 Usage

### Basic search

```python
from youtube_search_module import YouTubeConfig, YouTubeTools

config = YouTubeConfig(api_key="YOUR_KEY")
tools = YouTubeTools.create(config)
# Pass tools to your agent
```

### Restrict to a channel

```python
config = YouTubeConfig(
    api_key="YOUR_KEY",
    channel_id="UCQf_yRJpsfyEiWWpt1MZ6vA",  # restrict to this channel
    default_count=5,
)
tools = YouTubeTools.create(config)
```

### Access search directly (without agent)

```python
import asyncio
from youtube_search_module import YouTubeConfig, YouTubeSearch

async def main():
    config = YouTubeConfig(api_key="YOUR_KEY")
    searcher = YouTubeSearch(config)
    results = await searcher.search_async("AI agent framework", count=3)
    for r in results:
        print(r)

asyncio.run(main())
```

---

## ⚙️ Configuration

### `YouTubeConfig` fields

| Parameter | Type | Default | Description |
|---|---|---|---|
| `api_key` | `str` | *(required)* | YouTube Data API v3 key |
| `channel_id` | `str` | `""` | Optional channel ID to scope results |
| `max_results` | `int` | `25` | Upper bound on results the API may return per request |
| `default_count` | `int` | `10` | Default number of videos returned when count is not specified |
| `logger` | `Logger \| None` | `None` | Optional logger; falls back to `logging.getLogger(__name__)` |

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
