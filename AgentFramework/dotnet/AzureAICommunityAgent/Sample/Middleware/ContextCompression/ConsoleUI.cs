namespace ContextCompression;

internal static class ConsoleUI
{
    internal static void PrintBanner()
    {
        Println(ConsoleColor.Cyan, "╔══════════════════════════════════════════════════╗");
        Println(ConsoleColor.Cyan, "║  ContextCompressionMiddleware – Demo             ║");
        Println(ConsoleColor.Cyan, "╠══════════════════════════════════════════════════╣");
        Println(ConsoleColor.Cyan, "║  Demonstrates automatic history compression      ║");
        Println(ConsoleColor.Cyan, "║  using ContextCompressionMiddleware + Ollama     ║");
        Println(ConsoleColor.Cyan, "╚══════════════════════════════════════════════════╝");
    }

    internal static void PrintSection(string title) =>
        Println(ConsoleColor.Cyan, $"\n─── {title} {new string('─', Math.Max(0, 48 - title.Length))}");

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
