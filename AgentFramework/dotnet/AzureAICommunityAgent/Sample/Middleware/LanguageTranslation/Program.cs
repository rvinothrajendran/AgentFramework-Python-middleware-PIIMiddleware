using AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace LanguageTranslation;

internal class Program
{
    private const string Endpoint = "http://localhost:11434/";
    private const string Model    = "llama3.2";

    static async Task Main()
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(Endpoint);
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        IChatClient ollamaClient = new OllamaApiClient(httpClient, Model);

        PrintBanner();

        // ── Demo 1: LLM-only translation (no Azure credentials required) ──── //
        await RunLLMOnlyDemoAsync(ollamaClient);

        // ── Demo 2: Azure Translator + LLM Fallback ─────────────────────── //
        await RunAzureDemoAsync(ollamaClient);

        // ── Demo 3: Streaming with translation ──────────────────────────── //
        await RunStreamingDemoAsync(ollamaClient);

        Console.WriteLine();
        Println(ConsoleColor.Cyan, "Done.");
    }

    // ── LLM-only ─────────────────────────────────────────────────────────── //

    private static async Task RunLLMOnlyDemoAsync(IChatClient ollamaClient)
    {
        Console.WriteLine();
        Println(ConsoleColor.Cyan, "─── LLM Translation Only ────────────────────────────────");
        Println(ConsoleColor.Gray, "  Query : Wie heißt die Hauptstadt von Frankreich? (German)");

        var client = ollamaClient
            .AsBuilder()
            .Use(inner => LanguageTranslationMiddleware
                .CreateBuilder()
                .WithTargetLanguage("en")
                .WithLLMFallback(ollamaClient)
                .Build(inner))
            .Build();

        var messages = new[] { new ChatMessage(ChatRole.User, "Wie heißt die Hauptstadt von Frankreich?") };

        try
        {
            var response = await client.GetResponseAsync(messages);
            Println(ConsoleColor.Green, $"  Reply : {response.Messages[0].Text}");
        }
        catch (Exception ex)
        {
            Println(ConsoleColor.Red, $"  Error : {ex.Message}");
        }
    }

    // ── Azure + LLM Fallback ─────────────────────────────────────────────── //

    private static async Task RunAzureDemoAsync(IChatClient ollamaClient)
    {
        Console.WriteLine();
        Println(ConsoleColor.Cyan, "─── Azure Translator + LLM Fallback ─────────────────────");

        string? key;
        string? region;

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(region))
        {
            Println(ConsoleColor.Yellow,
                "  Skipped — set AZURE_TRANSLATOR_KEY and AZURE_TRANSLATOR_REGION to run this demo.");
            return;
        }

        var azureConfig = new AzureTranslatorConfig(Key: key, Region: region);

        Println(ConsoleColor.Gray, "  Query : Was ist die historische Bedeutung von Thanjavur?");

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

        var messages = new[] { new ChatMessage(ChatRole.User, "Was ist die historische Bedeutung von Thanjavur?") };

        try
        {
            var response = await client.GetResponseAsync(messages);
            Println(ConsoleColor.Green, $"  Reply : {response.Messages[0].Text}");
        }
        catch (Exception ex)
        {
            Println(ConsoleColor.Red, $"  Error : {ex.Message}");
        }
    }

    // ── Streaming ─────────────────────────────────────────────────────────── //

    private static async Task RunStreamingDemoAsync(IChatClient ollamaClient)
    {
        Console.WriteLine();
        Println(ConsoleColor.Cyan, "─── Streaming Translation ────────────────────────────────");
        Println(ConsoleColor.Gray, "  Query : ¿Cuál es la capital de España? (Spanish)");
        Print(ConsoleColor.White, "  Reply : ");

        var client = ollamaClient
            .AsBuilder()
            .Use(inner => LanguageTranslationMiddleware
                .CreateBuilder()
                .WithTargetLanguage("en")
                .WithLLMFallback(ollamaClient)
                .Build(inner))
            .Build();

        var messages = new[] { new ChatMessage(ChatRole.User, "¿Cuál es la capital de España?") };

        try
        {
            await foreach (var update in client.GetStreamingResponseAsync(messages))
                Print(ConsoleColor.White, update.Text);

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Println(ConsoleColor.Red, $"  Error : {ex.Message}");
        }
    }

    // ── Console helpers ───────────────────────────────────────────────────── //

    private static void PrintBanner()
    {
        Println(ConsoleColor.Cyan,  "╔══════════════════════════════════════════════════════╗");
        Println(ConsoleColor.Cyan,  "║   Language Translation Middleware — Sample App        ║");
        Println(ConsoleColor.Cyan,  "║   AzureAICommunity.Agent.Middleware · .NET 10         ║");
        Println(ConsoleColor.Cyan,  "╚══════════════════════════════════════════════════════╝");
    }

    private static void Println(ConsoleColor color, string text)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }

    private static void Print(ConsoleColor color, string text)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }
}
