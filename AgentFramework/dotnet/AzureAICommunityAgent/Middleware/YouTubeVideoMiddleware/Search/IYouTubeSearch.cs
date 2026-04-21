namespace AzureAICommunity.Agent.Middleware.YouTube.Search;

/// <summary>
/// Defines the contract for querying the YouTube Data API.
/// Implementations are responsible for building the HTTP request, executing it, and
/// returning the raw JSON response string for further processing.
/// </summary>
internal interface IYouTubeSearch
{
    /// <summary>
    /// Searches YouTube for videos matching the specified keywords.
    /// </summary>
    /// <param name="keyWords">Search terms to query.</param>
    /// <param name="count">Maximum number of results to request from the API.</param>
    /// <returns>A raw JSON string containing the API response, or an empty string on failure.</returns>
    Task<string> Search(string keyWords, int count);
}