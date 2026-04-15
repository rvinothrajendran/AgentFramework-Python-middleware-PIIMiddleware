<div align="center">

# AzureAICommunity — Agent Framework

Community packages that extend AI agent pipelines with reusable, plug-and-play capabilities.

[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

</div>

---

## Overview

This repository is a collection of community-contributed packages built on top of the **Agent Framework**. Each package targets a specific cross-cutting concern — such as language translation, PII protection, or security — and can be dropped into any agent pipeline independently or composed together.

Packages are organized by runtime:

| Runtime | Location | Status |
|---|---|---|
| Python | [`AgentFramework/Python/`](AgentFramework/Python/) | ✅ Available |
| .NET | [`AgentFramework/dotnet/`](AgentFramework/dotnet/) | ✅ Available |

---

## 🐍 Python Packages

All Python packages are published to [PyPI](https://pypi.org/search/?q=azureaicommunity-agent) and follow the same builder-pattern API.

📖 [Python Middleware overview →](AgentFramework/Python/Middleware/README.md)

### Available Packages

| Package | Version | Install | Description | Docs |
|---|---|---|---|---|
| `azureaicommunity-agent-language-middleware` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-language-middleware)](https://pypi.org/project/azureaicommunity-agent-language-middleware/) | `pip install`<br>`azureaicommunity-agent-language-middleware` | Language detection, translation, and back-translation for multi-lingual agent interactions | [README →](AgentFramework/Python/Middleware/AzureLanguage/language_middleware/README.md) |
| `azureaicommunity-agent-pii-middleware` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-pii-middleware)](https://pypi.org/project/azureaicommunity-agent-pii-middleware/) | `pip install`<br>`azureaicommunity-agent-pii-middleware` | PII detection and blocking before sensitive data reaches the LLM | [README →](AgentFramework/Python/Middleware/pii_middleware/README.md) |
| `azureaicommunity-agent-token-guard` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-token-guard)](https://pypi.org/project/azureaicommunity-agent-token-guard/) | `pip install`<br>`azureaicommunity-agent-token-guard` | Token usage tracking and quota enforcement per user, per period | [README →](AgentFramework/Python/Middleware/AgentTokenGuard/README.md) |

---

## 🔷 .NET Packages

All .NET packages are published to [NuGet](https://www.nuget.org/search?q=AzureAICommunity.Agent) and integrate with `Microsoft.Extensions.AI` via the `AsBuilder().Use(...)` pipeline pattern.

📖 [.NET Middleware overview →](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/README.md)

### Available Packages

| Package | Version | Install | Description | Docs |
|---|---|---|---|---|
| `AzureAICommunity.Agent`<br>`.Middleware.PIIChatDetectionMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware/) | `dotnet add package`<br>`AzureAICommunity.Agent`<br>`.Middleware.PIIChatDetectionMiddleware` | PII detection and enforcement (Allow / Mask / Block) before sensitive data reaches the LLM | [README →](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/PIIChatDetectionMiddleware/README.md) |
| `AzureAICommunity.Agent`<br>`.Middleware.TokenUsageMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware/) | `dotnet add package`<br>`AzureAICommunity.Agent`<br>`.Middleware.TokenUsageMiddleware` | Per-user token quota enforcement and detailed usage metrics for every AI agent completion call | [README →](AgentFramework/dotnet/AzureAICommunityAgent/Middleware/TokenUsageMiddleware/README.md) |

---

## 🤝 Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to add before submitting a pull request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 📄 License

MIT — see individual package LICENSE files for details.
