<div align="center">

# AzureAICommunity — Agent Framework

Community packages that extend AI agent pipelines with reusable, plug-and-play capabilities.

[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

</div>

---

## Overview

This repository is a collection of community-contributed packages built on top of the **Agent Framework**. Packages are organized into two types:

- **Middleware** — cross-cutting packages that intercept messages before and after they reach the LLM (language translation, PII detection, token quota enforcement, context compression)
- **Modules** — standalone tool packages that expose capabilities (file search, YouTube search) as `@tool`-decorated functions wired directly into any agent

All packages are independently installable and can be composed together in the same pipeline. Packages are available for both Python and .NET runtimes.

| Runtime | Middleware | Modules | Location |
|---|---|---|---|
| Python | 5 packages | 2 packages | [`AgentFramework/Python/`](AgentFramework/Python/) |
| .NET | 7 packages | — | [`AgentFramework/dotnet/`](AgentFramework/dotnet/) |

---

## 🐍 Python Packages

All Python packages are published to [PyPI](https://pypi.org/search/?q=azureaicommunity-agent) 

📖 [Python Middleware overview →](AgentFramework/Python/Middleware/README.md)

### Available Packages

---

#### 🌐 Language Middleware

[![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-language-middleware)](https://pypi.org/project/azureaicommunity-agent-language-middleware/) &nbsp; ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-language-middleware) &nbsp; [📖 README](AgentFramework/Python/Middleware/AzureLanguage/language_middleware/README.md)

`azureaicommunity-agent-language-middleware`

Language detection, translation, and back-translation for multi-lingual agent interactions.

---

#### 🔒 PII Middleware

[![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-pii-middleware)](https://pypi.org/project/azureaicommunity-agent-pii-middleware/) &nbsp; ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-pii-middleware) &nbsp; [📖 README](AgentFramework/Python/Middleware/pii_middleware/README.md)

`azureaicommunity-agent-pii-middleware`

PII detection and blocking before sensitive data reaches the LLM.

---

#### 📊 Token Guard

[![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-token-guard)](https://pypi.org/project/azureaicommunity-agent-token-guard/) &nbsp; ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-token-guard) &nbsp; [📖 README](AgentFramework/Python/Middleware/AgentTokenGuard/README.md)

`azureaicommunity-agent-token-guard`

Token usage tracking and quota enforcement per user, per period.

---

#### 🗜️ Context Compression

[![PyPI](https://img.shields.io/pypi/v/agentaicommunity-agent-context-compression)](https://pypi.org/project/agentaicommunity-agent-context-compression/) &nbsp; ![Downloads](https://static.pepy.tech/badge/agentaicommunity-agent-context-compression) &nbsp; [📖 README](AgentFramework/Python/Middleware/ContextCompression/README.md)

`agentaicommunity-agent-context-compression`

Automatic conversation history compression when token count approaches the context-window limit.

---

## 📦 Python Modules

📖 [Python Module overview →](AgentFramework/Python/Module/)

### Available Modules

---

#### 🔍 File Search

[![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-file-search)](https://pypi.org/project/azureaicommunity-agent-file-search/) &nbsp; ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-file-search) &nbsp; [📖 README](AgentFramework/Python/Module/FileModule/README.md)

`azureaicommunity-agent-file-search`

File search by name (glob) and content for AI agent pipelines.

---

#### ▶️ YouTube Search

[![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-youtube-search)](https://pypi.org/project/azureaicommunity-agent-youtube-search/) &nbsp; ![Downloads](https://static.pepy.tech/badge/azureaicommunity-agent-youtube-search) &nbsp; [📖 README](AgentFramework/Python/Module/YouTubeModule/README.md)

`azureaicommunity-agent-youtube-search`

YouTube video search tools for AI agent pipelines.

---

## 🔷 .NET Packages

All .NET packages are published to [NuGet](https://www.nuget.org/search?q=AzureAICommunity.Agent) and integrate with `Microsoft.Extensions.AI` via the `AsBuilder().Use(...)` pipeline pattern.

📖 [.NET Middleware overview →](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/README.md)

### Available Packages

---

#### 🌐 LanguageTranslationMiddleware

[![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware/) &nbsp; ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware) &nbsp; [📖 README](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/LanguageTranslationMiddleware/README.md)

`AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware`

Automatically detects the user's language, translates incoming messages to the agent's working language, and back-translates every response — supports 100+ languages via Azure Translator with an optional LLM fallback.

---

#### 🔒 PIIChatDetectionMiddleware

[![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware/) &nbsp; ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware) &nbsp; [📖 README](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/PIIChatDetectionMiddleware/README.md)

`AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware`

Scans every message for personally identifiable information and enforces a configurable policy (Allow / Mask / Block) before sensitive data reaches the LLM — detects emails, phones, credit cards, SSNs, and more.

---

#### 📊 TokenUsageMiddleware

[![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware/) &nbsp; ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware) &nbsp; [📖 README](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/TokenUsageMiddleware/README.md)

`AzureAICommunity.Agent.Middleware.TokenUsageMiddleware`

Tracks token consumption per user and enforces configurable quotas with callbacks for threshold breaches and exceeded limits — detailed usage metrics on every AI agent completion call.

---

#### 🗜️ ContextCompressionMiddleware

[![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware/) &nbsp; ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware) &nbsp; [📖 README](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/ContextCompressionMiddleware/README.md)

`AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware`

Automatically summarises older conversation history when the estimated token count approaches a configurable limit — prevents context-window overflow while preserving the most recent messages verbatim.

---

#### ▶️ YouTube Middleware

[![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.YouTube)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.YouTube/) &nbsp; ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.YouTube) &nbsp; [📖 README](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/YouTubeVideoMiddleware/README.md)

`AzureAICommunity.Agent.Middleware.YouTube`

Exposes a `SearchVideos` tool that queries the YouTube Data API v3 and returns matching video titles, descriptions, and watch URLs — wired into any agent via the `AsBuilder().Use(...)` pipeline.

---

#### 🔍 FileSearchMiddleware

[![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.FileSearchMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.FileSearchMiddleware/) &nbsp; ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.FileSearchMiddleware) &nbsp; [📖 README](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/FileSearchMiddleware/README.md)

`AzureAICommunity.Agent.Middleware.FileSearchMiddleware`

Provides `SearchByName` (glob pattern) and `SearchByContent` tools for searching the local file system — wired into any agent via the `AsBuilder().Use(...)` pipeline.

---

#### 🗺️ AzureMapsAddressSuggestionMiddleware

[![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware/) &nbsp; ![Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware) &nbsp; [📖 README](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/AzureMapsAddressSuggestionMiddleware/README.md)

`AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware`

Queries the Azure Maps Search API for points of interest and address suggestions anywhere in the world — wired into any agent via the `AsBuilder().Use(...)` pipeline.

---

## 🗺️ Roadmap

The following packages and improvements are planned for upcoming releases:

### 🐍 Python
| Package | Type | Description |
|---|---|---|
| `azureaicommunity-agent-azure-maps` | Module | Azure Maps POI and address suggestion tool for AI agent pipelines |
| `azureaicommunity-agent-memory-middleware` | Middleware | Persistent conversation memory with configurable storage backends (in-memory, file, Redis) |

### 🔷 .NET
| Package | Type | Description |
|---|---|---|
| `AzureAICommunity.Agent.Module.FileSearchModule` | Module | File search by name (glob) and content as a standalone module |
| `AzureAICommunity.Agent.Module.YouTubeSearchModule` | Module | YouTube video search as a standalone module |

> 💡 Have an idea for a new package? Open an issue or start a discussion — contributions are welcome!

---

## 🤝 Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to add before submitting a pull request.

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

MIT — see individual package LICENSE files for details.
