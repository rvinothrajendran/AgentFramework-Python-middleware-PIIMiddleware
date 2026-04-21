using Microsoft.Extensions.Logging;

namespace AzureAICommunity.Agent.Middleware.YouTube;

/// <summary>
/// Holds all configuration settings required to connect to and query the YouTube Data API.
/// Pass an instance of this class to <see cref="YouTubeTools"/> or <see cref="YouTubeFetch"/>
/// to control API credentials, target channel, and result-count limits.
/// </summary>
public sealed class YouTubeConfig
{
    /// <summary>YouTube Data API v3 key used to authenticate requests.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Optional YouTube channel ID to restrict search results to a specific channel.</summary>
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>Upper bound on the number of results the API may return per request.</summary>
    public int MaxResults { get; init; } = 25;

    /// <summary>Default number of videos to return when the caller does not specify a count.</summary>
    public int DefaultCount { get; init; } = 10;

    /// <summary>Optional logger factory used to create loggers throughout the middleware pipeline.</summary>
    public ILoggerFactory? LoggerFactory { get; init; }
}

