<div align="center">

# AzureAICommunity — Agent Framework (.NET)

Plug-and-play middleware packages for AI agent pipelines built on **Microsoft.Extensions.AI**.

[![License](https://img.shields.io/badge/license-MIT-blue)](../../LICENSE)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

</div>

All packages are published to [NuGet](https://www.nuget.org/search?q=AzureAICommunity.Agent).

📖 [Middleware package details →](AzureAICommunityAgent/Middleware/README.md)

---

## 📦 Available Packages

| Package | NuGet | Downloads | Description | Docs |
|---|---|---|---|---|
| `AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware/) | ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware) | Language detection, translation, and back-translation for multi-lingual agent interactions | [README →](AzureAICommunityAgent/Middleware/LanguageTranslationMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware/) | ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware) | PII detection and enforcement (Allow / Mask / Block) before sensitive data reaches the LLM | [README →](AzureAICommunityAgent/Middleware/PIIChatDetectionMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.TokenUsageMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware/) | ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware) | Per-user token quota enforcement and detailed usage metrics for every AI agent completion call | [README →](AzureAICommunityAgent/Middleware/TokenUsageMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware/) | ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware) | Automatic conversation history compression when token count approaches the context-window limit | [README →](AzureAICommunityAgent/Middleware/ContextCompressionMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.YouTube` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.YouTube)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.YouTube/) | ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.YouTube) | Search YouTube videos directly from your AI agent pipeline using the YouTube Data API v3 | [README →](AzureAICommunityAgent/Middleware/YouTubeVideoMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.FileSearchMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.FileSearchMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.FileSearchMiddleware/) | ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.FileSearchMiddleware) | File search by name (glob) and content for AI agent pipelines | [README →](AzureAICommunityAgent/Middleware/FileSearchMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware/) | ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware) | Find points of interest anywhere in the world from your AI agent pipeline using Azure Maps | [README →](AzureAICommunityAgent/Middleware/AzureMapsAddressSuggestionMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.ToolLimitMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.ToolLimitMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.ToolLimitMiddleware/) | ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.ToolLimitMiddleware) | Prevent runaway tool calls by enforcing a global cap and optional per-tool limits with usage introspection | [README →](AzureAICommunityAgent/Middleware/ToolLimitMiddleware/README.md) |

---

## 🌐 Language Translation Middleware

Automatically detects the user's language, translates incoming messages to the agent's target language, and back-translates responses — using Azure AI Translator with an optional LLM fallback.

- Automatic language detection with configurable confidence threshold
- Round-trip translation (user → agent language → user)
- Azure AI Translator backend (100+ languages)
- LLM fallback when Azure is unavailable
- Fluent `CreateBuilder()` API for easy composition
- Full streaming support (`GetStreamingResponseAsync`)
- Drops into any `Microsoft.Extensions.AI` pipeline

📦 `dotnet add package AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware`  
📖 [Full documentation](AzureAICommunityAgent/Middleware/LanguageTranslationMiddleware/README.md)

---

## 🛡️ PII Chat Detection Middleware

Scans every user message for personally identifiable information (PII) and enforces a configurable policy — **Allow**, **Mask**, or **Block** — before the request reaches the underlying chat client. Built on Microsoft Recognizers Text.

- Detects emails, phones, IPs, credit cards, numbers, dates, and dimensions
- Three enforcement policies: `Allow`, `Mask`, `Block`
- Fine-grained allow/block lists per PII type
- Full streaming support (`GetStreamingResponseAsync`)
- Drops into any `Microsoft.Extensions.AI` pipeline

📦 `dotnet add package AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware`  
📖 [Full documentation](AzureAICommunityAgent/Middleware/PIIChatDetectionMiddleware/README.md)

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
📖 [Full documentation](AzureAICommunityAgent/Middleware/TokenUsageMiddleware/README.md)

---

## 📺 YouTube Video Middleware

Let your AI agent find and return YouTube videos using the **YouTube Data API v3** — with a single line of middleware.

- Natural-language search returning ranked video results with titles, descriptions, and watch URLs
- Optional channel scoping to restrict results to a specific YouTube channel
- Built-in `count` and `offset` parameters for paged results
- Registers as an `AITool` callable by the agent automatically
- Structured logging via optional `ILoggerFactory`
- Drops into any `Microsoft.Extensions.AI` pipeline

📦 `dotnet add package AzureAICommunity.Agent.Middleware.YouTube`  
📖 [Full documentation](AzureAICommunityAgent/Middleware/YouTubeVideoMiddleware/README.md)

---

## 🔎 File Search Middleware

Exposes two `AITool` tools — `SearchByName` and `SearchByContent` — that an LLM agent can invoke to search the local file system.

- Glob pattern matching with auto-normalisation (`cs` → `*.cs`)
- Full-text content search with encoding fallback (`utf-8` → `latin-1`)
- Configurable depth, result cap, hidden-file skipping, and extension filters
- Binary file detection and symlink-loop guard
- `SearchConfig.DefaultPath` — set once, no path embedding needed in prompts
- Drops into any `Microsoft.Extensions.AI` pipeline

📦 `dotnet add package AzureAICommunity.Agent.Middleware.FileSearchMiddleware`  
📖 [Full documentation](AzureAICommunityAgent/Middleware/FileSearchMiddleware/README.md)

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
📖 [Full documentation](AzureAICommunityAgent/Middleware/ContextCompressionMiddleware/README.md)

---

## 🗺️ Azure Maps Address Suggestion Middleware

Enables your AI agent to find **real points of interest** (coffee shops, hospitals, pharmacies, ATMs, …) anywhere in the world using **Azure Maps**. The middleware geocodes the location to coordinates, then searches for actual POIs nearby — returning rich structured address data with only the fields your scenario needs.

- Two-step resolution: Geocoding API → Fuzzy Search API for accurate POI results
- Five field profiles: `Basic`, `Navigation`, `Display`, `Full`, or `Custom`
- Custom flag composition — cherry-pick exactly the fields to send to the LLM
- Culture-safe URL building — no locale-related 400 errors on non-English machines
- Drop-in middleware — one `.UseAzureMapsSearch()` call wires everything in
- Fully extensible via `IPoiSearchClient` — swap backends without changing the handler

📦 `dotnet add package AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware`  
📖 [Full documentation](AzureAICommunityAgent/Middleware/AzureMapsAddressSuggestionMiddleware/README.md)

---

## 🛠️ Tool Limit Middleware

Prevents **runaway tool calls** by enforcing a **global cap** and optional **per-tool limits** within a single agent session. Over-limit calls are silently removed, the model is notified via a user message, and a live `ToolUsageState` snapshot is always available via `IToolLimitTracker`.

- Global call cap — limits total tool invocations in a session
- Per-tool limits — independent ceilings per tool name
- Silent suppression — over-limit calls are removed; no exception is thrown
- Model notification — user-role message appended when calls are blocked
- `PerTool` (all attempted) and `PerToolAllowed` (allowed only) counters per tool
- `Reset()` clears all counters for a fresh session
- Full streaming support (`GetStreamingResponseAsync`)
- Drops into any `Microsoft.Extensions.AI` pipeline

📦 `dotnet add package AzureAICommunity.Agent.Middleware.ToolLimitMiddleware`  
📖 [Full documentation](AzureAICommunityAgent/Middleware/ToolLimitMiddleware/README.md)

---

## 👤 Author

Built and maintained by **Vinoth Rajendran**.

- 🐙 GitHub: [github.com/rvinothrajendran](https://github.com/rvinothrajendran) — _follow for more projects!_
- 📺 YouTube: [youtube.com/@VinothRajendran](https://www.youtube.com/@VinothRajendran) — _subscribe for tutorials and demos!_
- 💼 LinkedIn: [linkedin.com/in/rvinothrajendran](https://www.linkedin.com/in/rvinothrajendran/) — _let's connect!_

---

## 📄 License

MIT — see [LICENSE](../../LICENSE) for details.
