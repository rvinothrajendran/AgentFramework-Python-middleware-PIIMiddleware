<div align="center">

# 📺 AzureAICommunity – YouTube Video Middleware (.NET)

Search YouTube videos directly from your AI agent pipeline using the **YouTube Data API v3**.

[![NuGet Version](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.YouTube)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.YouTube/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.YouTube)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.YouTube/)
[![License](https://img.shields.io/github/license/rvinothrajendran/AgentFramework)](https://github.com/rvinothrajendran/AgentFramework/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![GitHub Repo](https://img.shields.io/badge/GitHub-AgentFramework-181717?logo=github)](https://github.com/rvinothrajendran/AgentFramework)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

**Let your AI agent

[Getting Started](#-installation) · [Configuration](#️-configuration) · [Usage](#-usage) · [How It Works](#️-how-it-works) · [Type Reference](#-type-reference) · [Contributing](#-contributing)

</div>

---

## Overview

`AzureAICommunity.Agent.Middleware.YouTube` is a plug-and-play YouTube search layer for AI agent pipelines built on `Microsoft.Agents.AI` and `Microsoft.Extensions.AI`. It exposes a `SearchVideos` AI tool that accepts a natural-language query, a result count, and an offset, and returns matching YouTube watch URLs with titles and descriptions — all driven by the YouTube Data API v3.

---

## ✨ Features

| | Feature |
|---|---|
| 🔍 | **Natural-language search** — pass any query and get ranked YouTube video results |
| 📺 | **Channel scoping** — optionally restrict results to a specific YouTube channel |
| 📄 | **Paged results** — built-in `count` and `offset` parameters for client-side paging |
| 🤖 | **AI tool integration** — registers as an `AITool` callable by the agent automatically |
| 🔌 | **Drop-in middleware** — one `.UseYouTubeSearch()` call wires everything into the pipeline |
| 🪵 | **Structured logging** — optional `ILoggerFactory` for trace-level diagnostics |
| 🛡️ | **Input validation** — safe defaults and argument guards throughout the pipeline |

---

## 📦 Installation

```bash
dotnet add package AzureAICommunity.Agent.Middleware.YouTube
```

---

## 🚀 Quick Start

```csharp
using AzureAICommunity.Agent.Middleware.YouTube;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

var apiKey = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY")!;

YouTubeConfig youTubeConfig = new YouTubeConfig
{
    ApiKey       = apiKey,
    ChannelId    = Environment.GetEnvironmentVariable("YOUTUBE_CHANNEL_ID") ?? string.Empty,
    MaxResults   = 25,
    DefaultCount = 5
};

var tools = YouTubeTools.Create(youTubeConfig);

IChatClient ollamaClient = new OllamaApiClient("http://localhost:11434/", "llama3.2");

AIAgent originalAgent = new ChatClientAgent(ollamaClient,
    instructions: """
        You are a helpful assistant with access to YouTube search.
        When the user asks for videos, always call the YouTube search tool.
        Always include the full watch URL (https://www.youtube.com/watch?v=...) in the results.
        """,
    tools: tools);

AIAgent agent = new AIAgentBuilder(originalAgent)
    .UseYouTubeSearch()
    .Build();

var response = await agent.RunAsync("Find me tutorials on Learn Microsoft AI");
Console.WriteLine(response);
```

---

## ⚙️ Configuration

All settings are provided through a `YouTubeConfig` instance:

| Property | Type | Default | Description |
|---|---|---|---|
| `ApiKey` | `string` | *(required)* | YouTube Data API v3 key used to authenticate requests |
| `ChannelId` | `string` | `""` | Optional channel ID to restrict results to a specific YouTube channel |
| `MaxResults` | `int` | `25` | Upper bound on the number of results the API may return per request |
| `DefaultCount` | `int` | `10` | Number of videos to return when the caller does not specify a count |
| `LoggerFactory` | `ILoggerFactory?` | `null` | Optional logger factory for trace-level diagnostics |

```csharp
YouTubeConfig config = new YouTubeConfig
{
    ApiKey       = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY")!,
    ChannelId    = "UCxxxxxxxxxxxxxx",   // Leave empty to search all of YouTube
    MaxResults   = 25,
    DefaultCount = 5,
    LoggerFactory = loggerFactory        // Optional; pass null to silence logs
};
```

---

## 🧑‍💻 Usage

### Middleware Pipeline

Register the middleware on an `AIAgentBuilder` so the agent automatically intercepts and handles `SearchVideos` tool calls:

```csharp
var tools = YouTubeTools.Create(youTubeConfig);

AIAgent agent = new AIAgentBuilder(
        new ChatClientAgent(ollamaClient, instructions: "...", tools: tools))
    .UseYouTubeSearch()
    .Build();

var response = await agent.RunAsync("Find me tutorials on Learn Microsoft AI");
Console.WriteLine(response);
```


---

## 🔑 Getting a YouTube Data API v3 Key

1. Go to [Google Cloud Console](https://console.cloud.google.com/).
2. Create or select a project and enable the **YouTube Data API v3**.
3. Under **Credentials**, create an **API key**.
4. Store the key in an environment variable:

```bash
# Windows PowerShell
$env:YOUTUBE_API_KEY = "YOUR_API_KEY_HERE"

# Linux / macOS
export YOUTUBE_API_KEY="YOUR_API_KEY_HERE"
```

> ⚠️ **Never commit your API key to source control.**

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
