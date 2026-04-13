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
| .NET | `AgentFramework/DotNet/` _(coming soon)_ | 🚧 Planned |

---

## 🐍 Python Packages

All Python packages are published to [PyPI](https://pypi.org/search/?q=azureaicommunity-agent) and follow the same builder-pattern API.

📖 [Python Middleware overview →](AgentFramework/Python/Middleware/README.md)

### Available Packages

| Package | Install | Description | Docs |
|---|---|---|---|
| `azureaicommunity-agent-language-middleware` | `pip install azureaicommunity-agent-language-middleware` | Language detection, translation, and back-translation for multi-lingual agent interactions | [README →](AgentFramework/Python/Middleware/AzureLanguage/language_middleware/README.md) |
| `azureaicommunity-agent-pii-middleware` | `pip install azureaicommunity-agent-pii-middleware` | PII detection and blocking before sensitive data reaches the LLM | [README →](AgentFramework/Python/Middleware/pii_middleware/README.md) |

---

## 🔷 .NET Packages

.NET packages are planned and will be published to [NuGet](https://www.nuget.org/). See the `AgentFramework/DotNet/` folder when available.


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
