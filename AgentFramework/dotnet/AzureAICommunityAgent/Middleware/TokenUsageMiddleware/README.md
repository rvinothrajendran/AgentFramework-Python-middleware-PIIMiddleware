<div align="center">

# 🪙 AzureAICommunity - Agent - Token Usage Middleware

Enforce **per-user token quotas** and capture **detailed usage metrics** across every AI agent completion call.

[![NuGet Version](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.TokenUsageMiddleware/)
[![License](https://img.shields.io/github/license/rvinothrajendran/AgentFramework)](https://github.com/rvinothrajendran/AgentFramework/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![GitHub Repo](https://img.shields.io/badge/GitHub-AgentFramework-181717?logo=github)](https://github.com/rvinothrajendran/AgentFramework)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

**Track

[Getting Started](#-installation) · [Quota Stores](#-quota-stores) · [Period Keys](#-period-keys) · [Callbacks](#-callbacks) · [Contributing](#-contributing)

</div>

---

## Overview

`AzureAICommunity.Agent.Middleware.TokenUsageMiddleware` is a plug-and-play quota and metering layer for AI agent pipelines built on `Microsoft.Extensions.AI`. Before every request it checks a user's accumulated token count against a configurable limit and throws a `QuotaExceededException` if exhausted. After every successful completion (streaming or non-streaming) it persists the token delta to an `IQuotaStore` and fires an optional `onUsage` callback with a `TokenUsageRecord`.

---

## ✨ Features

| | Feature |
|---|---|
| 🚦 | **Pre-call quota enforcement** — blocks requests before the LLM is ever called |
| 📊 | **Post-call usage recording** — emits a `TokenUsageRecord` after every completion |
| 🔀 | **Streaming support** — works with both `GetResponseAsync` and `GetStreamingResponseAsync` |
| 🗓️ | **Flexible quota periods** — built-in `Day`, `Week`, and `Month` helpers, or any custom delegate |
| 🗄️ | **Pluggable storage** — `InMemoryQuotaStore` for development; bring your own Redis/SQL backend |
| 🔔 | **Callbacks** — `onUsage` and `onQuotaExceeded` hooks for billing, logging, and alerting |
| 🔌 | **MEA integration** — drops directly into any `Microsoft.Extensions.AI` pipeline via `AsBuilder().Use(...)` |

---

## 📦 Installation

```bash
dotnet add package AzureAICommunity.Agent.Middleware.TokenUsageMiddleware
```

---

## 🚀 Quick Start

```csharp
using AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient ollamaClient = new OllamaApiClient("http://localhost:11434/", "llama3.2");

var quotaStore = new InMemoryQuotaStore();

IChatClient client = ollamaClient
    .AsBuilder()
    .Use(inner => new TokenUsageMiddleware(
        inner,
        quotaStore: quotaStore,
        quotaTokens: 500,
        onUsage: async (record, ct) =>
        {
            Console.WriteLine($"[{record.UserId}] used {record.TotalTokens} tokens " +
                              $"({record.UsedTokensAfterCall}/{record.QuotaTokens} total)");
            await Task.CompletedTask;
        }))
    .Build();

var options = new ChatOptions
{
    AdditionalProperties = new() { ["user_id"] = "Vinoth" }
};

var response = await client.GetResponseAsync("What is the capital of France?", options);
Console.WriteLine(response.Message.Text);
```

---

## 🗄️ Quota Stores

The middleware delegates all persistence to an `IQuotaStore`. Two implementations are available out of the box:

### `InMemoryQuotaStore`

Fast, zero-dependency store backed by an in-process dictionary. Suitable for development, testing, and single-process apps where quota data does not need to survive restarts.

```csharp
var quotaStore = new InMemoryQuotaStore();
```

### Custom / Persistent Store

For production or multi-process deployments, implement `IQuotaStore` with any backend (Redis, SQL, Azure Table Storage, etc.):

```csharp
public sealed class RedisQuotaStore : IQuotaStore
{
    public long GetUsage(string userId, string periodKey) { /* ... */ }
    public void AddUsage(string userId, string periodKey, long tokens) { /* ... */ }
}
```

---

## 🗓️ Period Keys

The quota is scoped to a **period key** — a string that resets the counter. Use the built-in `PeriodKeys` helpers or supply any custom delegate:

| Helper | Example output | Usage |
|---|---|---|
| `PeriodKeys.Month` | `"2026-04"` | Monthly quota (default) |
| `PeriodKeys.Week` | `"2026-W15"` | Weekly quota |
| `PeriodKeys.Day` | `"2026-04-14"` | Daily quota |

```csharp
// Weekly quota
var client = ollamaClient.AsBuilder()
    .Use(inner => new TokenUsageMiddleware(
        inner,
        quotaStore: quotaStore,
        quotaTokens: 1000,
        periodKeyFn: PeriodKeys.Week))
    .Build();

// Custom period (e.g. hourly)
var client = ollamaClient.AsBuilder()
    .Use(inner => new TokenUsageMiddleware(
        inner,
        quotaStore: quotaStore,
        quotaTokens: 200,
        periodKeyFn: () => DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH")))
    .Build();
```

---

## 🔔 Callbacks

### `onUsage` — Post-completion metrics

Fires after every successful completion with an immutable `TokenUsageRecord` snapshot:

```csharp
.Use(inner => new TokenUsageMiddleware(
    inner,
    quotaStore: quotaStore,
    quotaTokens: 500,
    onUsage: async (record, ct) =>
    {
        // Forward to a billing system, database, or telemetry sink
        Console.WriteLine(
            $"User={record.UserId} Period={record.PeriodKey} Model={record.Model} " +
            $"Input={record.InputTokens} Output={record.OutputTokens} Total={record.TotalTokens} " +
            $"Used={record.UsedTokensAfterCall}/{record.QuotaTokens} Streaming={record.IsStreaming}");
        await Task.CompletedTask;
    }))
```

### `onQuotaExceeded` — Pre-exception hook

Fires when the quota check fails, **before** `QuotaExceededException` is thrown, giving you a chance to log or alert:

```csharp
.Use(inner => new TokenUsageMiddleware(
    inner,
    quotaStore: quotaStore,
    quotaTokens: 500,
    onQuotaExceeded: async (info, ct) =>
    {
        Console.WriteLine(
            $"[QUOTA] User={info.UserId} has used {info.UsedTokens}/{info.QuotaTokens} tokens " +
            $"in period {info.PeriodKey}. Request blocked.");
        await Task.CompletedTask;
    }))
```

---

## 🧑‍💻 Custom User Identification

By default the middleware reads the `"user_id"` key from `ChatOptions.AdditionalProperties`, falling back to `"anonymous"`. Supply a custom `userIdGetter` delegate to integrate with your own identity system:

```csharp
.Use(inner => new TokenUsageMiddleware(
    inner,
    quotaStore: quotaStore,
    quotaTokens: 500,
    userIdGetter: (messages, options) =>
    {
        // e.g. extract from a JWT claim stored in AdditionalProperties
        return options?.AdditionalProperties?.TryGetValue("sub", out var sub) == true
            ? sub?.ToString() ?? "anonymous"
            : "anonymous";
    }))
```

---

## ⚙️ How It Works

```
1. Intercept     →  middleware captures the incoming request
2. Quota check   →  GetUsage(userId, periodKey) compared against quotaTokens
3. Block         →  if used >= quota: fire onQuotaExceeded, throw QuotaExceededException
4. Delegate      →  forward to the inner IChatClient (LLM is called)
5. Record usage  →  AddUsage(userId, periodKey, totalTokens)
6. Emit record   →  fire onUsage callback with a TokenUsageRecord snapshot
7. Return        →  response (or stream) is returned to the caller
```

---

## 📄 Constructor Reference

```csharp
public TokenUsageMiddleware(
    IChatClient inner,                                                  // Inner chat client to delegate to
    IQuotaStore quotaStore,                                             // Per-user token storage backend
    long quotaTokens,                                                   // Maximum tokens per user per period (> 0)
    Func<TokenUsageRecord, CancellationToken, Task>? onUsage = null,   // Post-completion callback
    Func<QuotaExceededInfo, CancellationToken, Task>? onQuotaExceeded = null, // Pre-exception callback
    Func<IEnumerable<ChatMessage>, ChatOptions?, string>? userIdGetter = null, // User ID extractor
    Func<string>? periodKeyFn = null                                    // Period key factory (default: PeriodKeys.Month)
)
```

---

## 📋 Type Reference

| Type | Description |
|---|---|
| `TokenUsageMiddleware` | The main `DelegatingChatClient` middleware |
| `IQuotaStore` | Interface for per-user, per-period token storage |
| `InMemoryQuotaStore` | In-process dictionary-backed quota store |
| `TokenUsageRecord` | Immutable usage snapshot passed to `onUsage` |
| `QuotaExceededException` | Thrown when a user's quota is exhausted |
| `QuotaExceededInfo` | Context passed to `onQuotaExceeded` before the exception is thrown |
| `PeriodKeys` | Static helpers: `Month()`, `Week()`, `Day()` |

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
