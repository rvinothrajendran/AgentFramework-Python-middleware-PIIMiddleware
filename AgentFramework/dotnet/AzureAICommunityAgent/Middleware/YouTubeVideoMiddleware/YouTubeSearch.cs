using AzureAICommunity.Agent.Middleware.YouTube.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace AzureAICommunity.Agent.Middleware.YouTube;

/// <summary>
/// Orchestrates a YouTube video search by delegating the raw HTTP call to the internal
/// <see cref="Search.IYouTubeSearch"/> implementation, deserializing the JSON response, and
/// projecting each item into a strongly-typed <see cref="YouTubeResponse"/>.
/// Logging is supported via an optional <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>.
/// </summary>
public sealed class YouTubeSearch
{
    private readonly IYouTubeSearch youTubeSearch;
    private readonly ILogger<YouTubeSearch> logger;

    public YouTubeSearch(string apiKey, string channelId = "",ILoggerFactory? loggerFactory = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey));
        }

        youTubeSearch = new Search.YouTubeSearch(apiKey, channelId);

        logger = loggerFactory is not null
            ? loggerFactory.CreateLogger<YouTubeSearch>()
            : NullLogger<YouTubeSearch>.Instance;
    }


    /// <summary>
    /// Executes a YouTube search and maps the raw API response to <see cref="YouTubeResponse"/> objects.
    /// </summary>
    /// <param name="query">Search keywords to send to the YouTube Data API.</param>
    /// <param name="count">Maximum number of results to retrieve from the API.</param>
    /// <param name="offset">Number of leading results to skip (client-side paging).</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>An enumerable of <see cref="YouTubeResponse"/> items, or an empty sequence when no results are found.</returns>
    public async Task<IEnumerable<YouTubeResponse>> SearchAsync(string query, int count = 10, int offset = 0,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentNullException(nameof(query));
        }

        var search = await youTubeSearch.Search(query, count);

        if (string.IsNullOrEmpty(search))
        {
            return Enumerable.Empty<YouTubeResponse>();
        }

        this.logger.LogTrace("Response content received: {Data}", search);

        search = search.Replace(": null", ": 0");

        List<YouTubeResponse> youTubeResponses = new List<YouTubeResponse>();
        
        List<YouTubeResult>? videoResult = JsonConvert.DeserializeObject<List<YouTubeResult>>(search);
        if (videoResult?.Count > 0)
        {
            foreach (var result in videoResult)
            {
                if (result?.Id?.VideoId != null)
                {
                    YouTubeResponse response = new();
                    response.Title = result.Snippet?.Title;
                    response.Description = result.Snippet?.Description;
                    
                    var id = result.Id.VideoId.ToString();
                    if (string.CompareOrdinal(id, "0") != 0)
                    {
                        response.YouTubeVideoUrl  = $"https://www.youtube.com/watch?v={result.Id.VideoId.ToString()}";
                    }
                    
                    youTubeResponses.Add(response);
                }
            }

            return youTubeResponses;
        }

        return Enumerable.Empty<YouTubeResponse>();
    }
}
