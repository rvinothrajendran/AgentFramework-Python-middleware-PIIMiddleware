namespace AzureAICommunity.Agent.Middleware.FileSearchMiddleware;

/// <summary>
/// Tool name constants derived from <see cref="FileSearchHandler"/> method names.
/// Ensures the registered tool names always match the middleware condition checks.
/// </summary>
internal static class FileSearchToolNames
{
    internal const string SearchByName    = nameof(FileSearchHandler.SearchByName);
    internal const string SearchByContent = nameof(FileSearchHandler.SearchByContent);
}
