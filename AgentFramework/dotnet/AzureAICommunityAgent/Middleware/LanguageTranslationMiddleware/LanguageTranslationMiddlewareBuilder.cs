using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware;

/// <summary>
/// Fluent builder for <see cref="LanguageTranslationMiddleware"/>.
/// Obtain an instance via <see cref="LanguageTranslationMiddleware.CreateBuilder"/>.
/// </summary>
public sealed class LanguageTranslationMiddlewareBuilder
{
    private string _targetLanguage = "en";
    private float _minConfidence = 0.8f;
    private AzureTranslatorConfig? _azureConfig;
    private IChatClient? _llmClient;

    internal LanguageTranslationMiddlewareBuilder() { }

    /// <summary>Sets the ISO 639-1 language code the agent should reason in (default: <c>"en"</c>).</summary>
    public LanguageTranslationMiddlewareBuilder WithTargetLanguage(string language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        _targetLanguage = language;
        return this;
    }

    /// <summary>
    /// Sets the minimum detection confidence score required to forward a translation (default: <c>0.8</c>).
    /// Messages whose detected confidence falls below this threshold are passed through untranslated.
    /// </summary>
    public LanguageTranslationMiddlewareBuilder WithMinConfidence(float minConfidence)
    {
        if (minConfidence is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(minConfidence), "Confidence must be between 0 and 1.");
        _minConfidence = minConfidence;
        return this;
    }

    /// <summary>Configures Azure AI Translator as the primary translation backend.</summary>
    public LanguageTranslationMiddlewareBuilder WithAzureTranslator(AzureTranslatorConfig config)
    {
        _azureConfig = config ?? throw new ArgumentNullException(nameof(config));
        return this;
    }

    /// <summary>
    /// Configures an <see cref="IChatClient"/> that is used as either the primary translation
    /// service (when no Azure config is set) or as a fallback when Azure fails.
    /// </summary>
    public LanguageTranslationMiddlewareBuilder WithLLMFallback(IChatClient llmClient)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        return this;
    }

    /// <summary>
    /// Builds and returns a configured <see cref="LanguageTranslationMiddleware"/> instance.
    /// </summary>
    /// <param name="inner">The inner <see cref="IChatClient"/> to wrap.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when neither an Azure config nor an LLM client has been supplied.
    /// </exception>
    public LanguageTranslationMiddleware Build(IChatClient inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        if (_azureConfig is null && _llmClient is null)
            throw new InvalidOperationException(
                "At least one translation backend must be configured. " +
                "Call WithAzureTranslator() and/or WithLLMFallback().");

        ITranslationService? azure = _azureConfig is not null
            ? new AzureTranslationService(_azureConfig)
            : null;

        ITranslationService? llm = _llmClient is not null
            ? new AzureLLMTranslationService(_llmClient)
            : null;

        return new LanguageTranslationMiddleware(
            inner,
            targetLanguage: _targetLanguage,
            minConfidence: _minConfidence,
            primaryService: azure ?? llm!,
            fallbackService: azure is not null ? llm : null);
    }
}
