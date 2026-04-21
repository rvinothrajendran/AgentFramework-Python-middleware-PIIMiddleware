namespace AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware;

/// <summary>
/// Abstraction for language detection and text translation.
/// Implement this interface to provide a custom translation backend,
/// or use the built-in <see cref="AzureTranslationService"/> or <see cref="AzureLLMTranslationService"/>.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Detects the language of the supplied <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The text whose language should be detected.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A tuple of (<c>languageCode</c>, <c>confidence</c>) where <c>languageCode</c> is an
    /// ISO 639-1 code (e.g. <c>"en"</c>, <c>"fr"</c>, <c>"ta"</c>) and <c>confidence</c>
    /// is a score in <c>[0, 1]</c>, or <see langword="null"/> when the backend does not
    /// report confidence.
    /// </returns>
    Task<(string Language, float? Confidence)> DetectLanguageAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Translates <paramref name="text"/> from <paramref name="sourceLanguage"/> to
    /// <paramref name="targetLanguage"/>.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="sourceLanguage">ISO 639-1 code of the source language.</param>
    /// <param name="targetLanguage">ISO 639-1 code of the target language.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The translated text, or <see langword="null"/> when translation fails gracefully.</returns>
    Task<string?> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);
}
