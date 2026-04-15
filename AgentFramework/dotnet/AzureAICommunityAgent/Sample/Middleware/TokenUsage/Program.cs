using AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace TokenUsage;

internal class Program
{
    const string Endpoint = "http://localhost:11434/";
    const string Model    = "llama3.2";

    static async Task Main(string[] args)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(Endpoint),
            Timeout     = TimeSpan.FromMinutes(5)
        };

        IChatClient ollamaClient = new OllamaApiClient(httpClient, Model);

        ConsoleUI.PrintBanner();

        // Each user gets a quota of 500 tokens per month.
        var quotaStore = new InMemoryQuotaStore();
        const long quota = 500;

        // ── Normal usage — logs each call's token consumption ── //
        await RunNormalUsageDemoAsync(ollamaClient, quotaStore, quota);

        // ── Quota exhaustion — pre-fills quota then makes a call ── //
        await RunQuotaExhaustionDemoAsync(quotaStore, quota, ollamaClient);

        // ── Streaming with quota tracking ─────────────────────────── //
        await RunStreamingQuotaDemoAsync(ollamaClient, quotaStore, quota);

        // ── JSON-backed quota store — survives restarts ───────────── //
        await RunJsonBackedQuotaStoreDemoAsync(ollamaClient, quota);

        Console.WriteLine();
        ConsoleUI.Println(ConsoleColor.Cyan, "Done.");
    }

    private static async Task RunStreamingQuotaDemoAsync(IChatClient ollamaClient, InMemoryQuotaStore quotaStore, long quota)
    {
        Console.WriteLine();
        ConsoleUI.Println(ConsoleColor.Cyan, "─── Streaming with Quota Tracking (Vinoth) ──────────────");

        var optionsVinoth = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "Vinoth" } };

        var streamingClient = ollamaClient
            .AsBuilder()
            .Use(inner => new TokenUsageMiddleware(
                inner,
                quotaStore: quotaStore,
                quotaTokens: quota,
                onUsage: OnStreamingUsageAsync))
            .Build();

        ConsoleUI.Print(ConsoleColor.White, "  → ");
        try
        {
            await foreach (var update in streamingClient.GetStreamingResponseAsync(
                               new[] { new ChatMessage(ChatRole.User, "Name a planet.") }, optionsVinoth))
            {
                ConsoleUI.Print(ConsoleColor.White, update.Text ?? string.Empty);
            }
            Console.WriteLine();
        }
        catch (QuotaExceededException ex)
        {
            Console.WriteLine();
            ConsoleUI.Println(ConsoleColor.Red, $"  ✗ Quota exceeded: {ex.Message}");
        }
    }

    private static async Task RunQuotaExhaustionDemoAsync(InMemoryQuotaStore quotaStore, long quota, IChatClient ollamaClient)
    {
        Console.WriteLine();
        ConsoleUI.Println(ConsoleColor.Cyan, "─── Quota Exhaustion (Vinoth) ──────────────────────────────");

        var period = PeriodKeys.Month();
        quotaStore.AddUsage("Vinoth", period, quota); // exhaust Vinoth's quota

        var enforcedClient = ollamaClient
            .AsBuilder()
            .Use(inner => new TokenUsageMiddleware(
                inner,
                quotaStore: quotaStore,
                quotaTokens: quota,
                onQuotaExceeded: OnQuotaExceededAsync))
            .Build();
        
        var chatClientAgent = new ChatClientAgent(enforcedClient);

        ChatClientAgentRunOptions runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                AdditionalProperties = new() { ["user_id"] = "Vinoth" }
            }
        };

        try
        {
            await chatClientAgent.RunAsync(new[] { new ChatMessage(ChatRole.User, "Hello!") }, options: runOptions);
        }
        catch (QuotaExceededException ex)
        {
            ConsoleUI.Println(ConsoleColor.Red, $"  ✗ Blocked as expected: {ex.Message}");
        }
    }

    private static async Task RunNormalUsageDemoAsync(IChatClient ollamaClient, InMemoryQuotaStore quotaStore, long quota)
    {
        Console.WriteLine();
        ConsoleUI.Println(ConsoleColor.Cyan, "─── Normal Usage with Token Monitoring (Vinoth, 3 calls) ─");

        var monitoringClient = ollamaClient
            .AsBuilder()
            .Use(inner => new TokenUsageMiddleware(
                inner,
                quotaStore: quotaStore,
                quotaTokens: quota,
                onUsage: OnUsageAsync))
            .Build();

        var agentRunOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                AdditionalProperties = new() { ["user_id"] = "Vinoth" }
            }
        };

        var chatClientAgent   = new ChatClientAgent(monitoringClient);

        for (int i = 1; i <= 3; i++)
        {
            try
            {
                var response = await chatClientAgent.RunAsync(new[] { new ChatMessage(ChatRole.User, $"What is {i} squared?") }, options: agentRunOptions);
                ConsoleUI.Println(ConsoleColor.White, $"  → {response}");
            }
            catch (QuotaExceededException ex)
            {
                ConsoleUI.Println(ConsoleColor.Red, $"  ✗ Quota exceeded: {ex.Message}");
                break;
            }
        }
    }

    private static async Task RunJsonBackedQuotaStoreDemoAsync(IChatClient ollamaClient, long quota)
    {
        Console.WriteLine();
        ConsoleUI.Println(ConsoleColor.Cyan, "─── JSON-Backed Quota Store (Vinoth) ───────────────────");

        const string jsonPath = "quota-store.json";

        // JsonFileQuotaStore loads existing totals from disk on construction,
        // so token usage accumulates across process restarts.
        var jsonStore = new JsonFileQuotaStore(jsonPath);

        ConsoleUI.Println(ConsoleColor.DarkGray,
            $"  ℹ persisting to: {Path.GetFullPath(jsonPath)}");
        ConsoleUI.Println(ConsoleColor.DarkGray,
            $"  ℹ usage before call: {jsonStore.GetUsage("Vinoth", PeriodKeys.Month())} tokens");

        var client = ollamaClient
            .AsBuilder()
            .Use(inner => new TokenUsageMiddleware(
                inner,
                quotaStore:  jsonStore,
                quotaTokens: quota,
                onUsage:     OnUsageAsync))
            .Build();

        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "Vinoth" } };

        try
        {
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "Tell me about LLM")], options);

            ConsoleUI.Println(ConsoleColor.White, $"  → {response.Text}");
            ConsoleUI.Println(ConsoleColor.DarkGray,
                $"  ℹ usage after call:  {jsonStore.GetUsage("Vinoth", PeriodKeys.Month())} tokens");
            ConsoleUI.Println(ConsoleColor.DarkGray,
                $"  ℹ check file:        {Path.GetFullPath(jsonPath)}");
        }
        catch (QuotaExceededException ex)
        {
            ConsoleUI.Println(ConsoleColor.Red, $"  ✗ Quota exceeded: {ex.Message}");
        }
    }

    // ── Callbacks ────────────────────────────────────────────────────── //

    static Task OnUsageAsync(TokenUsageRecord record, CancellationToken _)
    {
        ConsoleUI.Println(ConsoleColor.Green,
            $"  ✔ user={record.UserId}  tokens_this_call={record.TotalTokens}" +
            $"  running_total={record.UsedTokensAfterCall}/{record.QuotaTokens}" +
            $"  period={record.PeriodKey}");
        return Task.CompletedTask;
    }

    static Task OnStreamingUsageAsync(TokenUsageRecord record, CancellationToken _)
    {
        ConsoleUI.Println(ConsoleColor.Green,
            $"  ✔ streaming={record.IsStreaming}  tokens={record.TotalTokens}" +
            $"  running_total={record.UsedTokensAfterCall}/{record.QuotaTokens}");
        return Task.CompletedTask;
    }

    static Task OnQuotaExceededAsync(QuotaExceededInfo info, CancellationToken _)
    {
        ConsoleUI.Println(ConsoleColor.Yellow,
            $"  ⚠ Quota callback fired: user={info.UserId}" +
            $"  used={info.UsedTokens}  limit={info.QuotaTokens}");
        return Task.CompletedTask;
    }
}
