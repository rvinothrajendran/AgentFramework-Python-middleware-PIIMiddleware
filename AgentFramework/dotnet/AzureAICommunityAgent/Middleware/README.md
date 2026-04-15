<div align="center">

# AzureAICommunity - Agent Middleware Packages (.NET)

A collection of plug-and-play middleware packages for AI agent pipelines built on **Microsoft.Extensions.AI**.

[![License](https://img.shields.io/badge/license-MIT-blue)](../../../../LICENSE)

</div>

---

## Overview

This folder contains .NET middleware packages that extend `Microsoft.Extensions.AI` chat client pipelines with cross-cutting capabilities. Each package is independently installable via NuGet and can be composed together using the `AsBuilder().Use(...)` pipeline pattern.

| Package | NuGet | Description | Docs |
|---|---|---|---|
| `AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware/) | PII detection and enforcement (Allow / Mask / Block) before sensitive data reaches the LLM | [README →](PIIChatDetectionMiddleware/README.md) |
| `AzureAICommunity.Agent.Middleware.TokenUsageMiddleware` | [![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware/) | Per-user token quota enforcement and detailed usage metrics for every AI agent completion call | [README →](TokenUsageMiddleware/README.md) |

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
