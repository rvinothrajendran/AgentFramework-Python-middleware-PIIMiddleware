<div align="center">

# 🌐 AzureAICommunity – Language Translation Middleware (.NET)

Automatic language detection, translation, and response back-translation middleware for AI agent applications built on **Microsoft.Extensions.AI**.

[![NuGet](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/rvinothrajendran/AgentFramework/blob/main/LICENSE)
[![GitHub Repo](https://img.shields.io/badge/GitHub-AgentFramework-181717?logo=github)](https://github.com/rvinothrajendran/AgentFramework)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

**Your agent always

</div>

---

## Overview

`AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware` is a plug-and-play translation layer for `Microsoft.Extensions.AI` chat pipelines. It automatically detects the user's language, translates incoming messages to the agent's target language, and back-translates the response — using Azure AI Translator with an optional LLM agent as fallback.

---

## ✨ Features

| | Feature |
|---|---|
| 🔍 | **Language detection** — automatically detect the user's language with configurable confidence threshold |
| 🔄 | **Round-trip translation** — translate incoming messages and back-translate agent responses |
| ☁️ | **Azure Translator** — production-grade speed and accuracy with 100+ languages |
| 🤖 | **LLM fallback** — silently falls back to LLM translation when Azure is unavailable |
| 🔧 | **Builder pattern** — fluent `CreateBuilder()` API for easy composition |
| 🌊 | **Streaming support** — works with both standard and streaming `IChatClient` calls |
| 🔌 | **Drop-in middleware** — integrates directly into any `Microsoft.Extensions.AI` pipeline |

---

## 📦 Installation

```bash
dotnet add package AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware
```

---

## 🚀 Quick Start

```csharp
using AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient ollamaClient = new OllamaApiClient(new Uri("http://localhost:11434/"), "llama3.2");

// LLM-only translation (no Azure credentials required)
var client = ollamaClient
    .AsBuilder()
    .Use(inner => LanguageTranslationMiddleware
        .CreateBuilder()
        .WithTargetLanguage("en")
        .WithLLMFallback(ollamaClient)
        .Build(inner))
    .Build();

var messages = new[] { new ChatMessage(ChatRole.User, "Wie heißt die Hauptstadt von Frankreich?") };
var response = await client.GetResponseAsync(messages);
Console.WriteLine(response.Messages[0].Text); // Reply back-translated to German
```

---

## ⚙️ Configuration

| Parameter | Method | Default | Description |
|-----------|--------|---------|-------------|
| `targetLanguage` | `WithTargetLanguage(string)` | `"en"` | ISO 639-1 code of the language the agent reasons in |
| `minConfidence` | `WithMinConfidence(float)` | `0.8` | Minimum detection confidence to apply translation |
| `azureConfig` | `WithAzureTranslator(config)` | `null` | Azure Translator credentials (optional) |
| `llmClient` | `WithLLMFallback(client)` | `null` | LLM client used as translation fallback or primary service |

---

## 🧑‍💻 Usage

### Option 1 — Azure Translator + LLM Fallback (Recommended)

```csharp
var azureConfig = new AzureTranslatorConfig(
    Key:    Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY")!,
    Region: Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION")!
);

var client = ollamaClient
    .AsBuilder()
    .Use(inner => LanguageTranslationMiddleware
        .CreateBuilder()
        .WithAzureTranslator(azureConfig)
        .WithLLMFallback(ollamaClient)
        .WithTargetLanguage("en")
        .WithMinConfidence(0.8f)
        .Build(inner))
    .Build();
```

### Option 2 — LLM Only

```csharp
var client = ollamaClient
    .AsBuilder()
    .Use(inner => LanguageTranslationMiddleware
        .CreateBuilder()
        .WithLLMFallback(ollamaClient)
        .WithTargetLanguage("en")
        .Build(inner))
    .Build();
```

### Option 3 — Direct Construction

```csharp
var azureService  = new AzureTranslationService(azureConfig);
var llmService    = new LLMTranslationService(ollamaClient);

var middleware = new LanguageTranslationMiddleware(
    inner:           ollamaClient,
    primaryService:  azureService,
    targetLanguage:  "en",
    minConfidence:   0.8f,
    fallbackService: llmService);
```

---

## 🔄 How It Works

```
User Message (any language)
        │
        ▼
┌─────────────────────────────────┐
│   LanguageTranslationMiddleware │
│  1. Detect language             │
│  2. Translate → target lang     │
└──────────────┬──────────────────┘
               │
               ▼
       Inner IChatClient
       (reasons in target lang)
               │
               ▼
┌─────────────────────────────────┐
│   LanguageTranslationMiddleware │
│  3. Back-translate response     │
│     → user's original language  │
└─────────────────────────────────┘
               │
               ▼
  Response (user's language)
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

MIT © 2026 Vinoth Rajendran – AzureAICommunity
