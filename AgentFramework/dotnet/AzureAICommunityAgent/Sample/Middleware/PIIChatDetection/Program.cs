using AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace PIIChatDetection;

internal class Program
{
    const string Endpoint = "http://localhost:11434/";
    const string Model    = "llama3.2";

    const string PiiMessage =
        "Hi, my name is Vinoth. " +
        "Email: r.vinoth@live.com. " +
        "Phone: +91-800-555-0199. " +
        "IP: 192.168.1.1.";

    // Theme color per policy — chosen to stand out on a black background.
    static readonly Dictionary<PIIPolicy, ConsoleColor> PolicyColors = new()
    {
        [PIIPolicy.Allow] = ConsoleColor.Green,
        [PIIPolicy.Mask]  = ConsoleColor.Yellow,
        [PIIPolicy.Block] = ConsoleColor.Red,
    };

    static async Task Main(string[] args)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(Endpoint),
            Timeout     = TimeSpan.FromMinutes(5)
        };

        IChatClient ollamaClient = new OllamaApiClient(httpClient, Model);

        PrintBanner();
        PrintInputMessage();

        await RunAsync(ollamaClient, PIIPolicy.Allow);
        await RunAsync(ollamaClient, PIIPolicy.Mask);
        await RunAsync(ollamaClient, PIIPolicy.Block);
    }

    // ------------------------------------------------------------------ //
    //  Wraps the shared Ollama client with the requested policy and        //
    //  sends the same PII message so the effect of each policy is clear.   //
    // ------------------------------------------------------------------ //
    static async Task RunAsync(IChatClient innerClient, PIIPolicy policy)
    {
        var color  = PolicyColors[policy];
        int dashes = 39 - policy.ToString().Length;

        // ── top border ──────────────────────────────────────────────── //
        Println(color, $"┌─ Policy: {policy} {new string('─', dashes)}┐");

        var client = innerClient
            .AsBuilder()
            .Use(inner => new PIIChatDetectionMiddleware(inner, policy: policy))
            .Build();

        var agent    = new ChatClientAgent(client);
        var response = await agent.RunAsync(PiiMessage);

        // ── response line ────────────────────────────────────────────── //
        Print(color,                 "│  ");
        Print(ConsoleColor.DarkGray, "Response: ");
        Println(ConsoleColor.White,  response?.ToString() ?? string.Empty);

        // ── bottom border ────────────────────────────────────────────── //
        Println(color, $"└{new string('─', 50)}┘");
        Console.WriteLine();
    }

    // ── UI helpers ──────────────────────────────────────────────────── //

    static void PrintBanner()
    {
        Println(ConsoleColor.Cyan, "╔══════════════════════════════════════════════════╗");
        Println(ConsoleColor.Cyan, "║      PIIChatDetectionMiddleware – Policy Demo    ║");
        Println(ConsoleColor.Cyan, "╚══════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    static void PrintInputMessage()
    {
        Print(ConsoleColor.DarkGray, " Input  ");
        Println(ConsoleColor.White,  $"\"{PiiMessage}\"");
        Console.WriteLine();
    }

    /// <summary>Writes <paramref name="text"/> in <paramref name="color"/> without a trailing newline.</summary>
    static void Print(ConsoleColor color, string text)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    /// <summary>Writes <paramref name="text"/> in <paramref name="color"/> followed by a newline.</summary>
    static void Println(ConsoleColor color, string text)
    {
        Print(color, text);
        Console.WriteLine();
    }
}
