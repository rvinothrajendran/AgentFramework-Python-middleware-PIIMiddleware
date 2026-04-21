namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

/// <summary>
/// Tool name constants derived from <see cref="MapsSearchHandler"/> method names.
/// Ensures the registered tool names always match the middleware condition checks.
/// </summary>
internal static class MapsSearchToolNames
{
    internal const string SearchSuggestion = nameof(MapsSearchHandler.SearchSuggestionAsync);
}
