<div align="center">

# 🛠️ AzureAICommunity - Agent - Tool Limit Middleware

Prevent **runaway tool calls** by enforcing **global and per-tool call limits** across every AI agent completion.

[![NuGet Version](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.ToolLimitMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.ToolLimitMiddleware/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.ToolLimitMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.ToolLimitMiddleware/)
[![License](https://img.shields.io/github/license/rvinothrajendran/AgentFramework)](https://github.com/rvinothrajendran/AgentFramework/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![GitHub Repo](https://img.shields.io/badge/GitHub-AgentFramework-181717?logo=github)](https://github.com/rvinothrajendran/AgentFramework)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

[Getting Started](#-installation) · [Per-Tool Limits](#-per-tool-limits) · [Inspect Usage](#-inspect-usage) · [How It Works](#%EF%B8%8F-how-it-works) · [Contributing](#-contributing)

</div>

---

## Overview

`AzureAICommunity.Agent.Middleware.ToolLimitMiddleware` is a lightweight guard layer for AI agent pipelines built on `Microsoft.Extensions.AI`. During each completion it tracks every `FunctionCallContent` emitted by the model and silently suppresses any calls that breach a configurable **global cap** or an optional **per-tool cap**. When calls are suppressed, a user-role message is appended to the conversation so the model is aware that limits have been reached.

---

## ✨ Features

| | Feature |
|---|---|
| 🔢 | **Global call cap** — limits the total number of tool invocations in a session |
| 🔧 | **Per-tool limits** — set independent ceilings for individual tool names |
| 🔀 | **Streaming support** — works with both `GetResponseAsync` and `GetStreamingResponseAsync` |
| 🤫 | **Silent suppression** — over-limit calls are removed; no exception is thrown |
| 💬 | **Model notification** — a user message informs the model when calls have been removed |
| 📊 | **Usage introspection** — `GetCurrentUsage()` returns attempted vs allowed counts per tool, plus configured limits |
| 🔄 | **Resettable** — `Reset()` clears counters for a fresh session |
| 🔌 | **MEA integration** — drops directly into any `Microsoft.Extensions.AI` pipeline via `UseToolLimit()` |

---

## 📦 Installation

```bash
dotnet add package AzureAICommunity.Agent.Middleware.ToolLimitMiddleware
```

---

## 🚀 Quick Start

```csharp
using System.ComponentModel;
using AzureAICommunityAgent.Middleware.ToolLimiting;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient ollamaClient = new OllamaApiClient("http://localhost:11434/", "llama3.2");

var weatherTool = AIFunctionFactory.Create(
    ([Description("The location to get the weather for.")] string location)
        => $"The weather in {location} is cloudy with a high of 15\u00b0C.",
    "GetWeather");

IChatClient client = ollamaClient
    .AsBuilder()
    .UseToolLimit(new ToolLimits { GlobalMax = 5 })
    .Build();

AIAgent agent = new ChatClientAgent(client,
    instructions: "You are a helpful assistant with access to a weather tool.",
    tools: [weatherTool]);

var response = await agent.RunAsync("What is the weather like in Amsterdam?");
Console.WriteLine(response.Text);
```

---

## 🔧 Per-Tool Limits

In addition to the global cap, you can restrict individual tools independently:

```csharp
var weatherTool = AIFunctionFactory.Create(/* ... */, "GetWeather");
var youtubeTool = AIFunctionFactory.Create(/* ... */, "SearchVideos");

IChatClient client = ollamaClient
    .AsBuilder()
    .UseToolLimit(new ToolLimits
    {
        GlobalMax = 10,
        PerToolMax = new Dictionary<string, int>
        {
            ["GetWeather"]   = 3,
            ["SearchVideos"] = 2
        }
    })
    .Build();

AIAgent agent = new ChatClientAgent(client,
    instructions: "You are a helpful assistant.",
    tools: [weatherTool, youtubeTool]);
```

Any call to `GetWeather` beyond 3, or to `SearchVideos` beyond 2, is silently removed — even if the global limit has not been reached.

---

## 📊 Inspect Usage

After building the client with `UseToolLimit`, retrieve the tracker via `GetService<IToolLimitTracker>()`:

```csharp
IChatClient client = ollamaClient
    .AsBuilder()
    .UseToolLimit(new ToolLimits
    {
        GlobalMax = 5,
        PerToolMax = new Dictionary<string, int> { ["GetWeather"] = 3 }
    })
    .Build();

// Run the agent
AIAgent agent = new ChatClientAgent(client,
    instructions: "You are a helpful assistant with access to a weather tool.",
    tools: [weatherTool]);

var response = await agent.RunAsync("What is the weather like in Amsterdam?");
Console.WriteLine(response.Text);

// Retrieve the tracker from the pipeline
var tracker = client.GetService<IToolLimitTracker>();

ToolUsageState usage = tracker!.GetCurrentUsage();
Console.WriteLine($"Total allowed calls: {usage.TotalCalls} / {usage.GlobalLimit}");
Console.WriteLine("Per-tool usage (attempted / allowed / limit):");
foreach (var (tool, attempted) in usage.PerTool)
{
    usage.PerToolAllowed.TryGetValue(tool, out var allowed);
    usage.PerToolLimits.TryGetValue(tool, out var perMax);
    var limitText = perMax > 0 ? $" / {perMax}" : string.Empty;
    Console.WriteLine($"  {tool}: attempted={attempted}  allowed={allowed}{limitText}");
}

// Reset counters for a new session
tracker.Reset();
```

**Example output:**
```
Total allowed calls: 3 / 5
Per-tool usage (attempted / allowed / limit):
  GetWeather: attempted=5  allowed=3 / 3
```

---

## 📄 Constructor Reference

```csharp
public ToolLimitMiddleware(
    IChatClient innerClient,    // Inner chat client to delegate to
    ToolLimits? limits = null   // Limits configuration (defaults: GlobalMax = 10, no per-tool limits)
)
```

### Extension method

```csharp
builder.UseToolLimit(new ToolLimits
{
    GlobalMax = 10,
    PerToolMax = new Dictionary<string, int> { ["GetWeather"] = 3, ["SearchVideos"] = 2 }
});
```

---

## 📋 Type Reference

| Type | Description |
|---|---|
| `IToolLimitTracker` | Public interface for reading usage and resetting counters |
| `ToolLimits` | Configuration object: `GlobalMax` (default 10), `PerToolMax` (per-tool ceilings) |
| `ToolUsageState` | Snapshot from `GetCurrentUsage()` — see table below |
| `ToolLimitMiddlewareExtensions` | `UseToolLimit(ToolLimits?)` extension for `ChatClientBuilder` |

#### `ToolUsageState` properties

| Property | Type | Description |
|---|---|---|
| `TotalCalls` | `int` | Number of tool calls actually allowed through |
| `GlobalLimit` | `int` | Configured `GlobalMax` value |
| `PerTool` | `Dictionary<string, int>` | **All attempted** calls per tool, including blocked |
| `PerToolAllowed` | `Dictionary<string, int>` | Calls that were **allowed** per tool |
| `PerToolLimits` | `Dictionary<string, int>` | Configured `PerToolMax` ceilings |

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
