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
| `azureaicommunity-agent-token-guard` | [![PyPI](https://img.shields.io/pypi/v/azureaicommunity-agent-token-guard)](https://pypi.org/project/azureaicommunity-agent-token-guard/) | Token usage tracking and quota enforcement per user, per period | [README →](AgentTokenGuard/README.md) |

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

## 🪙 Token Guard Middleware

Tracks token usage per user and enforces configurable quotas — blocking calls that exceed the limit and invoking a callback when the threshold is breached.

- Per-user token quota enforcement (daily, weekly, monthly, or custom period)
- Pluggable `QuotaStore` — in-memory or bring your own (file, Redis, database, etc.)
- `on_usage` callback with full usage record after every call
- `on_quota_exceeded` callback when a user hits their limit
- Works with any `agent-framework` compatible LLM client

📦 `pip install azureaicommunity-agent-token-guard`  
📖 [Full documentation](AgentTokenGuard/README.md)

---

## 📄 License

MIT — see individual package LICENSE files for details.
