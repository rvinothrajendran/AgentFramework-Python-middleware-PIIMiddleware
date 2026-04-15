namespace TokenUsage;

internal static class ConsoleUI
{
    internal static void PrintBanner()
    {
        Println(ConsoleColor.Cyan, "╔══════════════════════════════════════════════════╗");
        Println(ConsoleColor.Cyan, "║    TokenUsageMiddleware – Quota Guard Demo       ║");
        Println(ConsoleColor.Cyan, "╠══════════════════════════════════════════════════╣");
        Println(ConsoleColor.Cyan, "║  Demonstrates per-user monthly token quotas      ║");
        Println(ConsoleColor.Cyan, "║  using InMemoryQuotaStore + Ollama (llama3.2)     ║");
        Println(ConsoleColor.Cyan, "╚══════════════════════════════════════════════════╝");
    }

    internal static void Println(ConsoleColor color, string text) => Write(color, text, newLine: true);
    internal static void Print(ConsoleColor color, string text)   => Write(color, text, newLine: false);

    private static void Write(ConsoleColor color, string text, bool newLine)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (newLine) Console.WriteLine(text);
        else         Console.Write(text);
        Console.ForegroundColor = prev;
    }
}
