<div align="center">

# 🔎 AzureAICommunity – File Search Middleware (.NET)

File search middleware (search by name and content) for **Microsoft.Extensions.AI** agent pipelines.

[![License](https://img.shields.io/github/license/rvinothrajendran/AgentFramework)](https://github.com/rvinothrajendran/AgentFramework/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![GitHub Repo](https://img.shields.io/badge/GitHub-AgentFramework-181717?logo=github)](https://github.com/rvinothrajendran/AgentFramework)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

</div>

## Overview

`AzureAICommunity.Agent.Middleware.FileSearchMiddleware` provides two `AITool` tools that an LLM agent can invoke to search the file system:

| Tool | Description |
|---|---|
| `SearchByName` | Search files whose names match a glob pattern |
| `SearchByContent` | Scan file contents for a plain-text string |

The middleware integrates with the `Microsoft.Agents.AI` pipeline via `AIAgentBuilder.UseFileSearch()`, so tool calls are intercepted and executed automatically without embedding paths in user prompts.

## Features

- Glob pattern matching with auto-normalisation (`cs` → `*.cs`)
- Full-text content search with encoding fallback (`utf-8` → `latin-1`)
- Configurable depth, result cap, hidden-file skipping, and extension filters
- Binary file detection (null-byte probe)
- Symlink-loop guard
- `SearchConfig.DefaultPath` — set once, no need to pass paths in every prompt

## Quick Start

```csharp
using AzureAICommunity.Agent.Middleware.FileSearchMiddleware;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient baseClient = new OllamaApiClient("http://localhost:11434/", "llama3.2");

// Configure once – DefaultPath means the LLM never needs a path in the prompt
var searchConfig = new SearchConfig
{
    MaxResults        = 50,
    MaxDepth          = 5,
    SkipHidden        = true,
    ExcludeExtensions = [".log", ".tmp", ".bin"],
    DefaultPath       = Path.GetFullPath("."),
};

var tools = FileSearchTools.Create(searchConfig);

AIAgent originalAgent = new ChatClientAgent(baseClient,
    instructions: """
        You are a file-search assistant with access to tools that can search the file system.
        Always invoke the appropriate tool based on what the user is asking for.
        Report every result returned by the tool without summarising or omitting any paths.
        """,
    tools: tools);

// Register the file-search middleware in the agent pipeline
AIAgent agent = new AIAgentBuilder(originalAgent)
    .UseFileSearch()
    .Build();

// Prompts are clean – no paths needed, DefaultPath handles it
var response = await agent.RunAsync("Find all C# files and list every path you find.");
Console.WriteLine(response);
```

## SearchConfig

| Property | Default | Description |
|---|---|---|
| `DefaultPath` | `"."` | Fallback root directory when the LLM does not supply a path argument |
| `MaxResults` | `200` | Maximum paths returned per call |
| `MaxFileSizeBytes` | `10 MB` | Files above this size are skipped during content search |
| `MaxDepth` | `20` | Maximum directory recursion depth |
| `BinaryCheckBytes` | `8192` | Bytes read to detect binary files |
| `FollowSymlinks` | `false` | Whether to follow symbolic links |
| `SkipHidden` | `false` | Skip dot-files and dot-directories |
| `IncludeExtensions` | `null` (all) | Whitelist of extensions (e.g. `[".cs", ".txt"]`) |
| `ExcludeExtensions` | `null` | Blacklist of extensions (e.g. `[".log", ".tmp"]`) |
| `Encodings` | `["utf-8", "latin-1"]` | Encoding fallback chain for content search |

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
