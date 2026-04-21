namespace AzureAICommunity.Agent.Middleware.YouTube;

/// <summary>
/// Centralised string constants for all YouTube AI tool names.
/// Using constants avoids magic strings when registering or dispatching tools.
/// </summary>
internal static class YouTubeToolNames
{
    /// <summary>Name of the tool that searches YouTube and returns matching video URLs.</summary>
    internal const string SearchVideos = "SearchVideos";
}


