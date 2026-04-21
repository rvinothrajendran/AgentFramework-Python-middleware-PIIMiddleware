using Azure;
using Azure.AI.Translation.Text;

namespace AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware;

/// <summary>
/// <see cref="ITranslationService"/> implementation backed by the Azure AI Translator service.
/// Provides production-grade language detection and translation across 100+ languages.
/// </summary>
public sealed class AzureTranslationService : ITranslationService
{
    private readonly TextTranslationClient _client;

    /// <summary>
    /// Initialises a new <see cref="AzureTranslationService"/> from the supplied configuration.
    /// </summary>
    /// <param name="config">Azure Translator credentials and region.</param>
    public AzureTranslationService(AzureTranslatorConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var credential = new AzureKeyCredential(config.Key);
        _client = new TextTranslationClient(credential, config.Region);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Language detection is performed by calling the Translate API without a source language,
    /// which causes Azure Translator to auto-detect and report the detected language in the response.
    /// </remarks>
    public async Task<(string Language, float? Confidence)> DetectLanguageAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        try
        {
            // Translate to English without specifying 'from', so Azure returns the DetectedLanguage.
            var response = await _client.TranslateAsync(
                targetLanguages: ["en"],
                content: [text],
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var item = response.Value?.FirstOrDefault()
                ?? throw new InvalidOperationException("Azure Translator returned no detection result.");

            if (item.DetectedLanguage is null)
                throw new InvalidOperationException("Azure Translator did not return a detected language.");

            return (item.DetectedLanguage.Language, item.DetectedLanguage.Confidence);
        }
        catch (RequestFailedException ex)
        {
            throw new InvalidOperationException(
                $"Azure detect_language failed ({ex.Status}): {ex.Message}", ex);
        }
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

        try
        {
            var response = await _client.TranslateAsync(
                targetLanguages: [targetLanguage],
                content: [text],
                sourceLanguage: sourceLanguage,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var item = response.Value?.FirstOrDefault()
                ?? throw new InvalidOperationException("Azure Translator returned no translation result.");

            return item.Translations.FirstOrDefault()?.Text;  // TranslationText.Text
        }
        catch (RequestFailedException ex)
        {
            throw new InvalidOperationException(
                $"Azure translate failed ({ex.Status}): {ex.Message}", ex);
        }
    }
}
