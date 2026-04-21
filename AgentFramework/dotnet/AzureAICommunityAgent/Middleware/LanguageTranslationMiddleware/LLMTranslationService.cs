using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware;

/// <summary>
/// <see cref="ITranslationService"/> implementation that uses an <see cref="IChatClient"/>
/// to perform language detection and translation via prompt engineering.
/// Use this as a primary service when no Azure credentials are available,
/// or as a fallback when the Azure service is temporarily unavailable.
/// </summary>
public sealed class AzureLLMTranslationService : ITranslationService
{
    private readonly IChatClient _chatClient;

    // Canonical language names keyed by ISO 639-1 code, mirroring the Python implementation.
    private static readonly IReadOnlyDictionary<string, string> LanguageNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["af"] = "Afrikaans",  ["ar"] = "Arabic",     ["bg"] = "Bulgarian",  ["bn"] = "Bengali",
            ["ca"] = "Catalan",    ["cs"] = "Czech",       ["cy"] = "Welsh",      ["da"] = "Danish",
            ["de"] = "German",     ["el"] = "Greek",       ["en"] = "English",    ["es"] = "Spanish",
            ["et"] = "Estonian",   ["fa"] = "Persian",     ["fi"] = "Finnish",    ["fr"] = "French",
            ["gu"] = "Gujarati",   ["he"] = "Hebrew",      ["hi"] = "Hindi",      ["hr"] = "Croatian",
            ["hu"] = "Hungarian",  ["hy"] = "Armenian",    ["id"] = "Indonesian", ["is"] = "Icelandic",
            ["it"] = "Italian",    ["ja"] = "Japanese",    ["ka"] = "Georgian",   ["kn"] = "Kannada",
            ["ko"] = "Korean",     ["lt"] = "Lithuanian",  ["lv"] = "Latvian",    ["mk"] = "Macedonian",
            ["ml"] = "Malayalam",  ["mr"] = "Marathi",     ["ms"] = "Malay",      ["mt"] = "Maltese",
            ["nl"] = "Dutch",      ["no"] = "Norwegian",   ["pa"] = "Punjabi",    ["pl"] = "Polish",
            ["pt"] = "Portuguese", ["ro"] = "Romanian",    ["ru"] = "Russian",    ["sk"] = "Slovak",
            ["sl"] = "Slovenian",  ["sq"] = "Albanian",    ["sr"] = "Serbian",    ["sv"] = "Swedish",
            ["sw"] = "Swahili",    ["ta"] = "Tamil",       ["te"] = "Telugu",     ["th"] = "Thai",
            ["tl"] = "Filipino",   ["tr"] = "Turkish",     ["uk"] = "Ukrainian",  ["ur"] = "Urdu",
            ["vi"] = "Vietnamese", ["zh"] = "Chinese",
        };

    private static readonly Regex LangCodePattern = new(@"\b([a-z]{2,3})\b", RegexOptions.Compiled);

    /// <summary>
    /// Creates a new <see cref="AzureLLMTranslationService"/> that uses the supplied chat client.
    /// </summary>
    /// <param name="chatClient">The <see cref="IChatClient"/> used to run detection and translation prompts.</param>
    public AzureLLMTranslationService(IChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns a confidence of <c>1.0</c> because LLM detection does not produce a numeric score.
    /// </remarks>
    public async Task<(string Language, float? Confidence)> DetectLanguageAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var prompt =
            $"Identify the language of the following text. " +
            $"Return only the ISO 639-1 language code (e.g. en, es, fr, de) with no explanations.\n\n" +
            $"Example:\nInput: Hallo\nOutput: de\n\n" +
            $"Input: {text}\nOutput:";

        var response = await _chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var raw = (response.Messages.LastOrDefault()?.Text ?? string.Empty)
            .Trim().ToLowerInvariant();

        var matches = LangCodePattern.Matches(raw);
        var lang = matches.Count > 0 ? matches[^1].Value : "unknown";

        return (lang, 1.0f);
    }

    /// <inheritdoc/>
    public async Task<string?> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguage);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguage);

        var sourceName = GetLanguageName(sourceLanguage);
        var targetName = GetLanguageName(targetLanguage);

        var prompt =
            $"Translate the following text from {sourceName} to {targetName}. " +
            $"Return only the translated text with no explanations, labels, or extra words.\n\n" +
            $"Example:\nInput: Hallo\nOutput: Hello\n\n" +
            $"Input: {text}\nOutput:";

        var response = await _chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return (response.Messages.LastOrDefault()?.Text ?? string.Empty).Trim();
    }

    private static string GetLanguageName(string code) =>
        LanguageNames.TryGetValue(code, out var name) ? name : code;
}
