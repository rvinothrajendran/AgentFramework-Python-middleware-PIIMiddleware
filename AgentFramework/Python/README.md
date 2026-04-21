<div align="center">

# AzureAICommunity - Agent Packages

A collection of plug-and-play middleware and module packages for AI agent pipelines built on the **Agent Framework**.

[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

</div>

---

## Overview

This folder contains middleware and module packages that extend `agent-framework` pipelines with reusable, plug-and-play capabilities. Each package is independently installable and can be composed together.

## 🔌 Middleware Packages

Cross-cutting middleware that intercepts messages before and after they reach the LLM.

| Package | PyPI | Downloads | Description | Docs |
|---|---|---|---|---|
| `azureaicommunity-agent-language-middleware` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-language-middleware)](https://pypi.org/project/azureaicommunity-agent-language-middleware/) | ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-language-middleware) | Automatic language detection, translation, and back-translation for multi-lingual agent interactions | [README →](Middleware/AzureLanguage/language_middleware/README.md) |
| `azureaicommunity-agent-pii-middleware` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-pii-middleware)](https://pypi.org/project/azureaicommunity-agent-pii-middleware/) | ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-pii-middleware) | PII detection and blocking before sensitive data reaches the LLM | [README →](Middleware/pii_middleware/README.md) |
| `azureaicommunity-agent-token-guard` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-token-guard)](https://pypi.org/project/azureaicommunity-agent-token-guard/) | ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-token-guard) | Token usage tracking and quota enforcement per user, per period | [README →](Middleware/AgentTokenGuard/README.md) |
| `agentaicommunity-agent-context-compression` | [![PyPI](https://img.shields.io/pypi/v/agentaicommunity-agent-context-compression)](https://pypi.org/project/agentaicommunity-agent-context-compression/) | ![Downloads](https://static.pepy.tech/badge/agentaicommunity-agent-context-compression) | Automatic conversation history compression when token count approaches the context-window limit | [README →](Middleware/ContextCompression/README.md) |

---

## 📦 Module Packages

Standalone tool modules that add capabilities (file search, YouTube search, etc.) to any agent as `@tool`-decorated functions.

| Package | PyPI | Downloads | Description | Docs |
|---|---|---|---|---|
| `azureaicommunity-agent-file-search` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-file-search)](https://pypi.org/project/azureaicommunity-agent-file-search/) | ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-file-search) | File search by name (glob) and content for AI agent pipelines | [README →](Module/FileModule/README.md) |
| `azureaicommunity-agent-youtube-search` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-youtube-search)](https://pypi.org/project/azureaicommunity-agent-youtube-search/) | ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-youtube-search) | YouTube video search tools for AI agent pipelines | [README →](Module/YouTubeModule/README.md) |

---

## 🌐 Language Middleware

Automatically detects the user's language, translates incoming messages to the agent's target language, and back-translates the response — using Azure Translator with an optional LLM fallback.

- Language detection with configurable confidence threshold
- Round-trip translation (incoming + response)
- Azure Translator (100+ languages) with LLM fallback
- Fluent `create()` builder API

📦 `pip install azureaicommunity-agent-language-middleware`  
📖 [Full documentation](Middleware/AzureLanguage/language_middleware/README.md)

---

## 🛡️ PII Middleware

Scans every user message for personally identifiable information (PII) and blocks it before it reaches the LLM — using Microsoft Recognizers Text with optional LLM-assisted validation.

- Detects emails, phones, IPs, credit cards, SSNs, and more
- Pre-built profiles: `strict`, `standard`, `financial`, `healthcare`
- Fluent builder API with custom block/allow lists
- Optional secondary LLM agent for edge-case validation

📦 `pip install azureaicommunity-agent-pii-middleware`  
📖 [Full documentation](Middleware/pii_middleware/README.md)

---

## 🪙 Token Guard Middleware

Tracks token usage per user and enforces configurable quotas — blocking calls that exceed the limit and invoking a callback when the threshold is breached.

- Per-user token quota enforcement (daily, weekly, monthly, or custom period)
- Pluggable `QuotaStore` — in-memory or bring your own (file, Redis, database, etc.)
- `on_usage` callback with full usage record after every call
- `on_quota_exceeded` callback when a user hits their limit
- Works with any `agent-framework` compatible LLM client

📦 `pip install azureaicommunity-agent-token-guard`  
📖 [Full documentation](Middleware/AgentTokenGuard/README.md)

---

## �️ Context Compression Middleware

Automatically **summarises older conversation history** when the estimated token count approaches a configurable limit — preventing context-window overflow while keeping the most recent messages intact.

- Pre-call token estimation using `tiktoken` with a configurable `trigger_ratio` (default 80 %)
- Configurable recent-message window preserved verbatim (`keep_last_messages`)
- Assistant + tool message pairs kept together during splitting
- `on_threshold_reached` callback — return `False` to block instead of compress
- Streaming support via transform/result hooks
- Works with any `agent-framework` compatible LLM client

📦 `pip install agentaicommunity-agent-context-compression`  
📖 [Full documentation](Middleware/ContextCompression/README.md)

---

## 📁 File Search Module

Exposes file search capabilities (by name/glob and content) as a `@tool`-decorated async function that can be wired into any `agent-framework` agent. Supports multi-root search, case-insensitive matching, and configurable result limits.

- Search files by glob pattern across multiple directories
- Search file contents with keyword matching
- Configurable max results per search
- Works with any `agent-framework` compatible LLM client

📦 `pip install azureaicommunity-agent-file-search`  
📖 [Full documentation](Module/FileModule/README.md)

---

## 📺 YouTube Search Module

Exposes YouTube Data API v3 search as a `@tool`-decorated async function. Agents can search YouTube by natural language query with optional channel scoping, result count, and paged offset support.

- Natural language YouTube video search via YouTube Data API v3
- Optional channel ID scoping to restrict results to a specific channel
- Configurable result count and client-side paging (offset)
- Works with any `agent-framework` compatible LLM client

📦 `pip install azureaicommunity-agent-youtube-search`  
📖 [Full documentation](Module/YouTubeModule/README.md)

---

## 👤 Author

Built and maintained by **Vinoth Rajendran**.

- 🐙 GitHub: [github.com/rvinothrajendran](https://github.com/rvinothrajendran) — _follow for more projects!_
- 📺 YouTube: [youtube.com/@VinothRajendran](https://www.youtube.com/@VinothRajendran) — _subscribe for tutorials and demos!_
- 💼 LinkedIn: [linkedin.com/in/rvinothrajendran](https://www.linkedin.com/in/rvinothrajendran/) — _let's connect!_

---

## 📄 License

MIT — see individual package LICENSE files for details.
