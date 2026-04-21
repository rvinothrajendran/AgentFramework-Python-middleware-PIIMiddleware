<div align="center">

# AzureAICommunity - Agent Middleware Packages (.NET)

A collection of plug-and-play middleware packages for AI agent pipelines built on **Microsoft.Extensions.AI**.

[![License](https://img.shields.io/badge/license-MIT-blue)](../../../../LICENSE)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

| Package | NuGet | Description | Docs |
|---|---|---|---|
| `AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware/) | PII detection and enforcement (Allow / Mask / Block) before sensitive data reaches the LLM | [README →](PIIChatDetectionMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.TokenUsageMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware/) | Per-user token quota enforcement and detailed usage metrics for every AI agent completion call | [README →](TokenUsageMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware/) | Automatic conversation history compression when token count approaches the context-window limit | [README →](ContextCompressionMiddleware/README.md) |

---

## 🛡️ PII Chat Detection Middleware

Scans every user message for personally identifiable information (PII) and enforces a configurable policy — **Allow**, **Mask**, or **Block** — before the request reaches the underlying chat client. Built on Microsoft Recognizers Text.

- Detects emails, phones, IPs, credit cards, numbers, dates, and dimensions
- Three enforcement policies: `Allow`, `Mask`, `Block`
- Fine-grained allow/block lists per PII type
- Full streaming support (`GetStreamingResponseAsync`)
- Drops into any `Microsoft.Extensions.AI` pipeline

📦 `dotnet add package AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware`  
📖 [Full documentation](PIIChatDetectionMiddleware/README.md)

---

## 🪙 Token Usage Middleware

Enforces **per-user token quotas** and captures **detailed usage metrics** across every AI agent completion call. Blocks requests before the LLM is ever called when a user's quota is exhausted, and emits a `TokenUsageRecord` after every successful completion.

- Pre-call quota enforcement with `QuotaExceededException` on exhaustion
- Post-call usage recording via `onUsage` callback
- Flexible quota periods — `Day`, `Week`, `Month`, or any custom delegate
- Pluggable storage — `InMemoryQuotaStore` for development; bring your own Redis/SQL backend
- `onQuotaExceeded` hook for logging and alerting before the exception is thrown
- Full streaming support (`GetStreamingResponseAsync`)
- Drops into any `Microsoft.Extensions.AI` pipeline

📦 `dotnet add package AzureAICommunity.Agent.Middleware.TokenUsageMiddleware`  
📖 [Full documentation](TokenUsageMiddleware/README.md)

---

## 🗜️ Context Compression Middleware

Automatically **summarises older conversation history** when the estimated token count approaches a configurable limit — preventing context-window overflow while keeping the most recent messages intact.

- Pre-call token estimation with a configurable `triggerRatio` (default 80 %)
- Configurable recent-message window preserved verbatim (`keepLastMessages`)
- Tool + assistant message pairs kept together during splitting
- `onThresholdReached` callback — return `false` to block instead of compress
- Pluggable token counter — defaults to `charCount / 4`; inject any tokeniser
- Optional separate summariser client (e.g. a cheaper model)
- Full streaming support (`GetStreamingResponseAsync`)
- Drops into any `Microsoft.Extensions.AI` pipeline

📦 `dotnet add package AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware`  
📖 [Full documentation](ContextCompressionMiddleware/README.md)

---

## 👤 Author

Built and maintained by **Vinoth Rajendran**.

- 🐙 GitHub: [github.com/rvinothrajendran](https://github.com/rvinothrajendran) — _follow for more projects!_
- 📺 YouTube: [youtube.com/@VinothRajendran](https://www.youtube.com/@VinothRajendran) — _subscribe for tutorials and demos!_
- 💼 LinkedIn: [linkedin.com/in/rvinothrajendran](https://www.linkedin.com/in/rvinothrajendran/) — _let's connect!_
