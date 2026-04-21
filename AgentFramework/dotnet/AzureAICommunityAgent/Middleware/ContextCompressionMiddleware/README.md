<div align="center">

# 🗜️ AzureAICommunity – Context Compression Middleware

A plug-and-play `Microsoft.Extensions.AI` middleware that automatically **summarises older conversation history** when the estimated token count approaches your configured limit — preventing context-window overflow while keeping the most recent messages intact.

[![License](https://img.shields.io/github/license/rvinothrajendran/AgentFramework)](https://github.com/rvinothrajendran/AgentFramework/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![GitHub Repo](https://img.shields.io/badge/GitHub-AgentFramework-181717?logo=github)](https://github.com/rvinothrajendran/AgentFramework)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

Long-running conversations accumulate history. Every message is sent to the LLM on every turn, so token usage grows linearly. Eventually you hit the model's context-window limit and requests start failing.

</div>

## The Solution

`ContextCompressionMiddleware` intercepts the request **before** it reaches the LLM. When the estimated token count exceeds a configurable trigger threshold, the middleware:

1. Splits the history into **old** and **recent** segments.
2. Calls the LLM to produce a **compact summary** of the old segment.
3. Replaces the old segment with a single `system` summary message.
4. Forwards the compressed history to the inner client as normal.

---

## Features

| Feature | Detail |
|---|---|
| Automatic compression | Triggers pre-call when estimated tokens ≥ `triggerRatio × maxTokens` |
| Configurable threshold | `maxTokens` + `triggerRatio` (default 80 %) |
| Recent-message preservation | Last `keepLastMessages` messages always kept verbatim |
| Tool-pair awareness | Assistant + Tool messages are kept together when splitting |
| Threshold callback | Optional `onThresholdReached` — return `false` to block instead of compress |
| Custom token counter | Inject any tokeniser; defaults to `charCount / 4` approximation |
| Separate summariser | Optional dedicated `IChatClient` for summary calls |
| Streaming support | Works with both `GetResponseAsync` and `GetStreamingResponseAsync` |

---

## Installation

```bash
dotnet add package AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware
```

---

## Quick Start

```csharp
using AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient ollamaClient = new OllamaApiClient(new Uri("http://localhost:11434/"), "llama3.2");

var client = ollamaClient
    .AsBuilder()
    .Use(inner => new ContextCompressionMiddleware(
        inner,
        maxTokens: 4000,
        triggerRatio: 0.80,
        keepLastMessages: 6))
    .Build();

// Use like any other IChatClient — compression is transparent
var response = await client.GetResponseAsync(conversationHistory);
```

---

## Configuration

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `maxTokens` | `int` | `8000` | Token budget for the conversation |
| `triggerRatio` | `double` | `0.80` | Compression triggers at this fraction of `maxTokens` |
| `keepLastMessages` | `int` | `8` | Recent messages kept verbatim after compression |
| `onThresholdReached` | `Func<CompressionInfo, bool>?` | `null` | Optional callback; return `false` to block |
| `tokenCounter` | `Func<IEnumerable<ChatMessage>, int>?` | `null` | Custom token estimator |
| `summarizerClient` | `IChatClient?` | `null` | Dedicated client for summarisation calls |

### Threshold Callback

```csharp
var client = ollamaClient
    .AsBuilder()
    .Use(inner => new ContextCompressionMiddleware(
        inner,
        maxTokens: 4000,
        onThresholdReached: info =>
        {
            Console.WriteLine($"Threshold hit: {info.TokensUsed} / {info.MaxTokens} tokens");
            return true; // true = compress, false = throw ContextCompressionThresholdException
        }))
    .Build();
```

### Custom Token Counter

```csharp
// Example using a precise tokeniser
var client = ollamaClient
    .AsBuilder()
    .Use(inner => new ContextCompressionMiddleware(
        inner,
        maxTokens: 4000,
        tokenCounter: messages =>
        {
            // supply your own counting logic here
            return messages.Sum(m => m.Text?.Length / 4 ?? 0);
        }))
    .Build();
```

### Separate Summariser Client

```csharp
// Use a cheaper model for summarisation
IChatClient summarizerClient = new OllamaApiClient(new Uri("http://localhost:11434/"), "phi3");

var client = expensiveClient
    .AsBuilder()
    .Use(inner => new ContextCompressionMiddleware(
        inner,
        maxTokens: 8000,
        summarizerClient: summarizerClient))
    .Build();
```


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
