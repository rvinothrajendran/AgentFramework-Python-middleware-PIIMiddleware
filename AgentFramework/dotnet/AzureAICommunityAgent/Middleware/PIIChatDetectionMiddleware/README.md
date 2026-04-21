<div align="center">

# 🛡️ AzureAICommunity - Agent - PII Chat Detection Middleware

Secure AI agent pipelines by detecting and controlling **personally identifiable information (PII)** before it reaches the AI model.

[![NuGet Version](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware/)
[![License](https://img.shields.io/github/license/rvinothrajendran/AgentFramework)](https://github.com/rvinothrajendran/AgentFramework/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![GitHub Repo](https://img.shields.io/badge/GitHub-AgentFramework-181717?logo=github)](https://github.com/rvinothrajendran/AgentFramework)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

**Intercept

[Getting Started](#-installation) · [Policies](#️-pii-policies) · [Customization](#-custom-entity-lists) · [Contributing](#-contributing)

</div>

---

## Overview

`AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware` is a plug-and-play security layer for AI agent pipelines built on `Microsoft.Extensions.AI`. It scans every user message for PII using Microsoft's [Recognizers Text](https://github.com/microsoft/Recognizers-Text) library and enforces a configurable policy — **Allow**, **Mask**, or **Block** — before the request reaches the underlying chat client.

---

## ✨ Features

| | Feature |
|---|---|
| 🔍 | **PII detection** — emails, phones, IPs, credit cards, numbers, dates, dimensions |
| ⚙️ | **Three enforcement policies** — `Allow`, `Mask`, or `Block` detected PII |
| 🎛️ | **Allow & block lists** — fine-grained control over which PII types are enforced |
| 🔀 | **Streaming support** — works with both `GetResponseAsync` and `GetStreamingResponseAsync` |
| 🔌 | **MEA integration** — drops directly into any `Microsoft.Extensions.AI` pipeline via `AsBuilder().Use(...)` |

---

## 📦 Installation

```bash
dotnet add package AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware
```

---

## 🚀 Quick Start

```csharp
using AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient ollamaClient = new OllamaApiClient("http://localhost:11434/", "llama3.2");

IChatClient client = ollamaClient
    .AsBuilder()
    .Use(inner => new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Block))
    .Build();

var response = await client.GetResponseAsync("My email is user@example.com");
Console.WriteLine(response.Message.Text);
// → "Message blocked due to sensitive data: email"
```

---

## ⚙️ PII Policies

Choose how the middleware handles detected PII:

| Policy | Behaviour |
|---|---|
| `PIIPolicy.Allow` | Passes all messages through without modification (detection is skipped). |
| `PIIPolicy.Mask` | Replaces each PII span with a typed placeholder token (e.g. `<EMAIL_1>`, `<PHONE_NUMBER_2>`). |
| `PIIPolicy.Block` | Short-circuits the pipeline and returns an assistant message listing the blocked PII types. |

```csharp
// Allow all PII through
var client = ollamaClient.AsBuilder()
    .Use(inner => new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Allow))
    .Build();

// Mask PII before forwarding
var client = ollamaClient.AsBuilder()
    .Use(inner => new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask))
    .Build();

// Block any message containing PII
var client = ollamaClient.AsBuilder()
    .Use(inner => new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Block))
    .Build();
```

---

## 🔧 Custom Entity Lists

Fine-tune which PII types are enforced using `allowList` and `blockList`:

```csharp
// Only enforce policy on emails and phone numbers;
// leave everything else untouched
var client = ollamaClient.AsBuilder()
    .Use(inner => new PIIChatDetectionMiddleware(
        inner,
        blockList: ["email", "phonenumber"],
        policy: PIIPolicy.Block))
    .Build();

// Always let datetime values through, block everything else
var client = ollamaClient.AsBuilder()
    .Use(inner => new PIIChatDetectionMiddleware(
        inner,
        allowList: ["datetimeV2.date", "datetimeV2.datetime", "datetimeV2.time"],
        policy: PIIPolicy.Mask))
    .Build();
```

> If `blockList` is empty, the policy is applied to **all** detected PII types (minus anything in `allowList`).

---

## 🔍 Detected PII Types

The middleware uses Microsoft Recognizers Text to identify the following entity types:

| Entity | Example |
|---|---|
| `email` | `user@example.com` |
| `phonenumber` | `+1-555-0100` |
| `ip` | `192.168.1.1` |
| `creditcard` | `4111 1111 1111 1111` |
| `number` | `42`, `3.14` |
| `datetimeV2.date` / `datetimeV2.datetime` | `tomorrow at 3pm`, `2026-04-14` |
| `dimension` | `5 kg`, `10 miles` |

---

## ⚙️ How It Works

```
1. Intercept   →  middleware captures the last user message
2. Detect      →  Recognizers Text extracts entity spans
3. Filter      →  entities in allowList are removed from candidates
4. Match       →  blockList (if set) narrows candidates further
5. Enforce     →  remaining candidates are subject to the policy:
                   Allow  → pass through unchanged
                   Mask   → replace spans with <TYPE_N> tokens
                   Block  → return error response; LLM is never called
```

---

## 📄 Constructor Reference

```csharp
public PIIChatDetectionMiddleware(
    IChatClient inner,
    IEnumerable<string>? allowList = null,   // PII types to always pass through
    IEnumerable<string>? blockList = null,   // PII types to enforce policy on (empty = all)
    PIIPolicy policy = PIIPolicy.Block,      // Enforcement policy
    string culture = Culture.English         // Language/culture for recognizers
)
```

---

## 🤝 Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to change before submitting a pull request.

📁 **Repository:** [https://github.com/rvinothrajendran/AgentFramework](https://github.com/rvinothrajendran/AgentFramework)

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

MIT
