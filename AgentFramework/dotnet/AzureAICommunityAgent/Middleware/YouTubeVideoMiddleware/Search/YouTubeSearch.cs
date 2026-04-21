using System.Text.Json;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace AzureAICommunity.Agent.Middleware.YouTube.Search;

/// <summary>
/// Concrete implementation of <see cref="IYouTubeSearch"/> that uses the Google YouTube Data API v3
/// client library to issue search requests. Results are serialised to a JSON string and returned
/// for deserialization by the calling layer. An optional channel ID can be supplied to restrict
/// searches to a specific YouTube channel.
/// </summary>
internal class YouTubeSearch : IYouTubeSearch
{
    private readonly YouTubeService youtubeService;
    private string chennalId;
    public YouTubeSearch(string apiKey,string channelId = "")
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException(nameof(apiKey));

        youtubeService= new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = apiKey,
            ApplicationName = GetType().ToString(),
        });
        this.chennalId = channelId;
    }

    /// <summary>
    /// Sends a search request to the YouTube Data API and returns the serialised JSON response.
    /// If the channel ID was provided at construction time it is applied as a filter.
    /// Any exception thrown by the API client is caught and an empty string is returned.
    /// </summary>
    /// <param name="keyWords">Search terms to query.</param>
    /// <param name="count">Maximum number of results to request (mapped to <c>MaxResults</c>).</param>
    /// <returns>A JSON string of search result items, or an empty string on error.</returns>
    public async Task<string> Search(string keyWords, int count = 10)
    {
        if (string.IsNullOrEmpty(keyWords))
            throw new ArgumentNullException(nameof(keyWords));

        string result;
        try
        {
            var searchListRequest = youtubeService.Search.List("snippet");

            if (!string.IsNullOrEmpty(chennalId) && !string.IsNullOrWhiteSpace(chennalId))
            {
                searchListRequest.ChannelId = chennalId;
            }

            searchListRequest.MaxResults = count;

            searchListRequest.Q = keyWords;

            var searchListResponse = await searchListRequest.ExecuteAsync();

            var options = new JsonSerializerOptions { WriteIndented = true };
            result = JsonSerializer.Serialize(searchListResponse.Items, options);
            
        }
        catch (Exception)
        {
            result = string.Empty;
        }

        return result;
    }
}