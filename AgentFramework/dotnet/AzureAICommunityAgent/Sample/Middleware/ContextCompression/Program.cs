using AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace ContextCompression;

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

        // ── Demo 1: Normal long conversation — compression triggers transparently ── //
        await RunCompressionDemoAsync(ollamaClient);

        // ── Demo 2: Threshold callback — block request instead of compressing ────── //
        await RunBlockingCallbackDemoAsync(ollamaClient);

        // ── Demo 3: Streaming with auto-compression ──────────────────────────────── //
        await RunStreamingCompressionDemoAsync(ollamaClient);

        // ── Demo 4: Custom token counter ─────────────────────────────────────────── //
        await RunCustomTokenCounterDemoAsync(ollamaClient);

        Console.WriteLine();
        ConsoleUI.Println(ConsoleColor.Cyan, "Done.");
    }

    // ── Demo 1 ───────────────────────────────────────────────────────────────── //

    private static async Task RunCompressionDemoAsync(IChatClient ollamaClient)
    {
        ConsoleUI.PrintSection("Auto-Compression Demo");
        ConsoleUI.Println(ConsoleColor.DarkGray,
            "  Building a long conversation that will trigger compression (maxTokens=200)...");

        // Use a low maxTokens to trigger compression quickly in the demo
        var client = ollamaClient
            .AsBuilder()
            .Use(inner => new ContextCompressionMiddleware(
                inner,
                maxTokens: 200,
                triggerRatio: 0.80,
                keepLastMessages: 4,
                onThresholdReached: info =>
                {
                    ConsoleUI.Println(ConsoleColor.Yellow,
                        $"\n  ⚡ Compression triggered! tokens={info.TokensUsed} / max={info.MaxTokens}");
                    return true; // allow compression
                }))
            .Build();

        // Seed the conversation with pre-existing history
        var history = BuildConversationHistory();

        ConsoleUI.Println(ConsoleColor.DarkGray,
            $"  Pre-loaded {history.Count} messages into history.");

        // Add a new user question — this will trigger compression
        history.Add(new ChatMessage(ChatRole.User, "Given everything we discussed, what is the main takeaway?"));

        try
        {
            var response = await client.GetResponseAsync(history);
            ConsoleUI.Println(ConsoleColor.White, $"\n  → {response.Text}");
        }
        catch (Exception ex)
        {
            ConsoleUI.Println(ConsoleColor.Red, $"  ✗ Error: {ex.Message}");
        }
    }

    // ── Demo 2 ───────────────────────────────────────────────────────────────── //

    private static async Task RunBlockingCallbackDemoAsync(IChatClient ollamaClient)
    {
        ConsoleUI.PrintSection("Blocking Callback Demo");
        ConsoleUI.Println(ConsoleColor.DarkGray,
            "  The onThresholdReached callback returns false — request is blocked.");

        var client = ollamaClient
            .AsBuilder()
            .Use(inner => new ContextCompressionMiddleware(
                inner,
                maxTokens: 200,
                triggerRatio: 0.80,
                keepLastMessages: 4,
                onThresholdReached: info =>
                {
                    ConsoleUI.Println(ConsoleColor.Yellow,
                        $"  ⚡ Threshold hit ({info.TokensUsed} tokens). Blocking request.");
                    return false; // block — raise ContextCompressionThresholdException
                }))
            .Build();

        var history = BuildConversationHistory();
        history.Add(new ChatMessage(ChatRole.User, "Summarize our conversation."));

        try
        {
            await client.GetResponseAsync(history);
        }
        catch (ContextCompressionThresholdException ex)
        {
            ConsoleUI.Println(ConsoleColor.Red,
                $"  ✗ Blocked as expected: {ex.Message}");
        }
    }

    // ── Demo 3 ───────────────────────────────────────────────────────────────── //

    private static async Task RunStreamingCompressionDemoAsync(IChatClient ollamaClient)
    {
        ConsoleUI.PrintSection("Streaming with Auto-Compression");

        var client = ollamaClient
            .AsBuilder()
            .Use(inner => new ContextCompressionMiddleware(
                inner,
                maxTokens: 200,
                triggerRatio: 0.80,
                keepLastMessages: 4,
                onThresholdReached: info =>
                {
                    ConsoleUI.Println(ConsoleColor.Yellow,
                        $"\n  ⚡ Streaming compression triggered! tokens={info.TokensUsed}");
                    return true;
                }))
            .Build();

        var history = BuildConversationHistory();
        history.Add(new ChatMessage(ChatRole.User, "What was the most important decision we made?"));

        ConsoleUI.Print(ConsoleColor.White, "  → ");
        try
        {
            await foreach (var update in client.GetStreamingResponseAsync(history))
            {
                ConsoleUI.Print(ConsoleColor.White, update.Text ?? string.Empty);
            }
            Console.WriteLine();
        }
        catch (ContextCompressionThresholdException ex)
        {
            ConsoleUI.Println(ConsoleColor.Red, $"  ✗ Blocked: {ex.Message}");
        }
    }

    // ── Demo 4 ───────────────────────────────────────────────────────────────── //

    private static async Task RunCustomTokenCounterDemoAsync(IChatClient ollamaClient)
    {
        ConsoleUI.PrintSection("Custom Token Counter");
        ConsoleUI.Println(ConsoleColor.DarkGray,
            "  Using a custom token counter (message count × 20 = estimated tokens).");

        // Custom counter: each message is treated as 20 tokens regardless of content
        int CountByMessages(IEnumerable<ChatMessage> msgs) => msgs.Count() * 20;

        var client = ollamaClient
            .AsBuilder()
            .Use(inner => new ContextCompressionMiddleware(
                inner,
                maxTokens: 100,    // 5+ messages trigger compression (5 × 20 = 100 ≥ 80)
                triggerRatio: 0.80,
                keepLastMessages: 2,
                tokenCounter: CountByMessages,
                onThresholdReached: info =>
                {
                    ConsoleUI.Println(ConsoleColor.Yellow,
                        $"  ⚡ Custom counter triggered: {info.TokensUsed} estimated tokens.");
                    return true;
                }))
            .Build();

        var history = new List<ChatMessage>
        {
            new(ChatRole.User,      "Tell me about the solar system."),
            new(ChatRole.Assistant, "The solar system consists of the Sun and eight planets."),
            new(ChatRole.User,      "Which planet is largest?"),
            new(ChatRole.Assistant, "Jupiter is the largest planet."),
            new(ChatRole.User,      "What about Saturn?"),
        };

        try
        {
            var response = await client.GetResponseAsync(history);
            ConsoleUI.Println(ConsoleColor.White, $"  → {response.Text}");
        }
        catch (Exception ex)
        {
            ConsoleUI.Println(ConsoleColor.Red, $"  ✗ Error: {ex.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────── //

    /// <summary>Builds a long pre-existing conversation history for demos.</summary>
    private static List<ChatMessage> BuildConversationHistory()
    {
        return new List<ChatMessage>
        {
            new(ChatRole.User,      "We're building a new product called AgentKit. The goal is to simplify AI agent development."),
            new(ChatRole.Assistant, "That sounds exciting! What key problems does AgentKit aim to solve for developers?"),
            new(ChatRole.User,      "Mainly the complexity of managing memory, tool use, and context windows."),
            new(ChatRole.Assistant, "Context window management is indeed a significant pain point. Have you considered automatic summarisation?"),
            new(ChatRole.User,      "Yes, that's one of the middleware components we're designing."),
            new(ChatRole.Assistant, "Good call. You'll also want to think about token counting accuracy and latency impact."),
            new(ChatRole.User,      "We settled on a character-based approximation as the default, with a pluggable interface for precision tokenisers."),
            new(ChatRole.Assistant, "Smart. That keeps the dependency footprint small while allowing power users to swap in tiktoken-style counting."),
            new(ChatRole.User,      "We also decided to keep the last 6 messages verbatim to preserve conversation flow."),
            new(ChatRole.Assistant, "Six messages should be enough context for most turn-by-turn dialogues."),
        };
    }
}
