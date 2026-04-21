namespace AzureAICommunity.Agent.Middleware.YouTube;

/// <summary>
/// Provides a high-level helper for searching YouTube videos.
/// <see cref="SearchVideosAsync"/> validates inputs, applies safe paging defaults, delegates
/// the actual HTTP request to <see cref="YouTubeSearch"/>, and returns a sliced page of
/// <see cref="YouTubeResponse"/> objects ready for consumption by agent tools.
/// </summary>
public static class YouTubeFetch
{
    /// <summary>
    /// Searches YouTube for videos matching <paramref name="query"/> and returns a paged slice of results.
    /// </summary>
    /// <param name="query">Natural-language search keywords.</param>
    /// <param name="count">Number of videos to return; falls back to <see cref="YouTubeConfig.DefaultCount"/> when &lt;= 0.</param>
    /// <param name="offset">Zero-based number of results to skip before selecting the page.</param>
    /// <param name="config">Configuration object supplying the API key, channel filter, and result limits.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A list of <see cref="YouTubeResponse"/> items representing matching videos.</returns>
    public static async Task<List<YouTubeResponse>> SearchVideosAsync(
        string query,
        int count,
        int offset,
        YouTubeConfig config,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("query must be a non-empty string.", nameof(query));

        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new ArgumentException("ApiKey must be set in YouTubeConfig.", nameof(config));

        var safeOffset = Math.Max(0, offset);
        var maxResults = Math.Max(1, config.MaxResults);
        var safeCount = Math.Clamp(count <= 0 ? config.DefaultCount : count, 1, maxResults);
        var fetchCount = Math.Min(maxResults, safeOffset + safeCount);

        var search = new YouTubeSearch(config.ApiKey, config.ChannelId, config.LoggerFactory);
        var urls = await search.SearchAsync(query, fetchCount, 0, cancellationToken);

        return urls
            .Skip(safeOffset)
            .Take(safeCount)
            .ToList();
    }
}

