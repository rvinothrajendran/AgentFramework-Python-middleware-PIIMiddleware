namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

/// <summary>
/// Abstraction for point-of-interest (POI) / category searches near coordinates.
/// </summary>
/// <remarks>
/// <para>
/// The current implementation (<see cref="FuzzySearchClient"/>) calls the Azure Maps
/// Fuzzy Search REST API directly because the <c>Azure.Maps.Search</c> SDK v2 beta
/// does not expose a POI search method.
/// </para>
/// <para>
/// When a future SDK version adds typed POI support, replace <see cref="FuzzySearchClient"/>
/// with a new implementation of this interface — <see cref="MapsSearchHandler"/> requires
/// no changes.
/// </para>
/// </remarks>
internal interface IPoiSearchClient
{
    /// <summary>
    /// Searches for <paramref name="query"/> (e.g. "coffee shop") near the given
    /// coordinates and returns up to <paramref name="maxResults"/> matches mapped
    /// to <see cref="Address"/> objects.
    /// </summary>
    /// <param name="query">Category or name to search for (e.g. "pharmacy", "atm").</param>
    /// <param name="lat">Latitude of the search centre.</param>
    /// <param name="lon">Longitude of the search centre.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="fallbackLocation">Location string used when no formatted address is available in the result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched addresses, or <see langword="null"/> when nothing is found.</returns>
    Task<List<Address>?> SearchAsync(
        string            query,
        double            lat,
        double            lon,
        int               maxResults,
        string            fallbackLocation,
        CancellationToken cancellationToken = default);
}
