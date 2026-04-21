namespace FileSearchAgentDemo;

internal static class ConsoleUI
{
    internal static void PrintBanner()
    {
        Println(ConsoleColor.Cyan, "╔══════════════════════════════════════════════════╗");
        Println(ConsoleColor.Cyan, "║  FileSearchModule – Agent Demo                   ║");
        Println(ConsoleColor.Cyan, "╠══════════════════════════════════════════════════╣");
        Println(ConsoleColor.Cyan, "║  AIAgent + FileSearchTools                  ║");
        Println(ConsoleColor.Cyan, "╚══════════════════════════════════════════════════╝");
    }

    internal static void PrintSection(string title) =>
        Println(ConsoleColor.Cyan, $"\n─── {title} {new string('─', Math.Max(0, 48 - title.Length))}");

    internal static void Println(ConsoleColor color, string text) => Write(color, text, newLine: true);

    private static void Write(ConsoleColor color, string text, bool newLine)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (newLine) Console.WriteLine(text);
        else         Console.Write(text);
        Console.ForegroundColor = prev;
    }
}
