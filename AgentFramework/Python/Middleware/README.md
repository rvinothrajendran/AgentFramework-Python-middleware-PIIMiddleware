<div align="center">

# AzureAICommunity - Agent Middleware Packages

A collection of plug-and-play middleware packages for AI agent pipelines built on the **Agent Framework**.

[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

</div>

---

## Overview

This folder contains middleware packages that extend `agent-framework` pipelines with cross-cutting capabilities. Each package is independently installable and can be composed together in the same pipeline.

| Package | PyPI | Description | Docs |
|---|---|---|---|
| `azureaicommunity-agent-language-middleware` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-language-middleware)](https://pypi.org/project/azureaicommunity-agent-language-middleware/) | Automatic language detection, translation, and back-translation for multi-lingual agent interactions | [README →](AzureLanguage/language_middleware/README.md) |
| `azureaicommunity-agent-pii-middleware` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-pii-middleware)](https://pypi.org/project/azureaicommunity-agent-pii-middleware/) | PII detection and blocking before sensitive data reaches the LLM | [README →](pii_middleware/README.md) |

---

## 🌐 Language Middleware

Automatically detects the user's language, translates incoming messages to the agent's target language, and back-translates the response — using Azure Translator with an optional LLM fallback.

- Language detection with configurable confidence threshold
- Round-trip translation (incoming + response)
- Azure Translator (100+ languages) with LLM fallback
- Fluent `create()` builder API

📦 `pip install azureaicommunity-agent-language-middleware`  
📖 [Full documentation](AzureLanguage/language_middleware/README.md)

---

## 🛡️ PII Middleware

Scans every user message for personally identifiable information (PII) and blocks it before it reaches the LLM — using Microsoft Recognizers Text with optional LLM-assisted validation.

- Detects emails, phones, IPs, credit cards, SSNs, and more
- Pre-built profiles: `strict`, `standard`, `financial`, `healthcare`
- Fluent builder API with custom block/allow lists
- Optional secondary LLM agent for edge-case validation

📦 `pip install azureaicommunity-agent-pii-middleware`  
📖 [Full documentation](pii_middleware/README.md)

---

## 📄 License

MIT — see individual package LICENSE files for details.
