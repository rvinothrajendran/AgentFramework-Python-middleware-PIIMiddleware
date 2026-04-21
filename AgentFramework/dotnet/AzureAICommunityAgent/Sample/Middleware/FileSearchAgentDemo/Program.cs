using AzureAICommunity.Agent.Middleware.FileSearchMiddleware;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace FileSearchAgentDemo;

internal class Program
{
    const string Endpoint = "http://localhost:11434/";
    const string Model    = "llama3.2";

    static async Task Main(string[] args)
    {
        ConsoleUI.PrintBanner();

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(Endpoint),
            Timeout     = TimeSpan.FromMinutes(5)
        };

        IChatClient baseClient = new OllamaApiClient(httpClient, Model);

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

        AIAgent agent = new AIAgentBuilder(originalAgent)
            .UseFileSearch()
            .Build();

        await RunAsync(agent, "Demo 1 – Knowledge + File Search",
            "Give me a brief history of Chennai, then search for all C# files and list every path you find.");

        await RunAsync(agent, "Demo 1b – Find by Name (txt only)",
            "Find all text files and list every file path.");

        await RunAsync(agent, "Demo 1c – Find files by name prefix",
            "Find all files whose name starts with 'File' and list every path.");

        await RunAsync(agent, "Demo 2 – Find by Content",
            "Find all files whose content mentions 'SearchConfig' and list each file path.");

        Console.WriteLine();
        ConsoleUI.Println(ConsoleColor.Cyan, "Done.");
    }

    private static async Task RunAsync(AIAgent agent, string title, string prompt)
    {
        ConsoleUI.PrintSection(title);
        ConsoleUI.Println(ConsoleColor.DarkGray, $"  > {prompt}");

        try
        {
            var response = await agent.RunAsync(prompt);
            ConsoleUI.Println(ConsoleColor.White, $"\n  LLM: {response}");
        }
        catch (Exception ex)
        {
            ConsoleUI.Println(ConsoleColor.Red, $"  ✗ {ex.Message}");
        }
    }
}
