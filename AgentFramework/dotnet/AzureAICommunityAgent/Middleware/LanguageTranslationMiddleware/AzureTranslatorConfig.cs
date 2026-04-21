namespace AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware;

/// <summary>
/// Credentials and endpoint configuration for the Azure AI Translator service.
/// </summary>
/// <param name="Key">The Azure Translator subscription key.</param>
/// <param name="Region">The Azure region where the Translator resource is hosted (e.g. <c>"eastus"</c>).</param>
/// <param name="Endpoint">
///   Optional custom endpoint URL. Defaults to <c>"https://api.cognitive.microsofttranslator.com"</c>
///   when <see langword="null"/> or empty, which is correct for most multi-service and regional keys.
/// </param>
public sealed record AzureTranslatorConfig(
    string Key,
    string Region,
    string? Endpoint = null)
{
    internal static readonly string DefaultEndpoint = "https://api.cognitive.microsofttranslator.com";

    /// <summary>Returns the effective endpoint, falling back to the global default when not set.</summary>
    public string EffectiveEndpoint =>
        string.IsNullOrWhiteSpace(Endpoint) ? DefaultEndpoint : Endpoint;
}
