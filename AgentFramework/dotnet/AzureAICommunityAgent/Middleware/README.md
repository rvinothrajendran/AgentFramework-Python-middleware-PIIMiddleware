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
