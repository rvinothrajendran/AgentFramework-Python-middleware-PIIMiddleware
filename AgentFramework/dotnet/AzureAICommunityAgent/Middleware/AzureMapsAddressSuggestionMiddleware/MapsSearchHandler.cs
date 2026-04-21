using Azure;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;

namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

/// <summary>
/// Core Azure Maps search logic used by <see cref="MapsSearchToolFactory"/>.
/// Can also be called directly without an AI pipeline.
/// </summary>
public static class MapsSearchHandler
{
    private static MapsSearchConfig  _config    = new();
    private static MapsSearchClient? _client;
    private static IPoiSearchClient  _poiClient = new FuzzySearchClient(string.Empty);

    internal static void Configure(MapsSearchConfig config)
    {
        _config    = config;
        _client    = new MapsSearchClient(new AzureKeyCredential(config.AzureMapsKey));
        _poiClient = new FuzzySearchClient(config.AzureMapsKey);
    }

    /// <summary>
    /// Allows tests (or a future SDK upgrade) to inject a custom <see cref="IPoiSearchClient"/>
    /// without touching the rest of the handler.
    /// </summary>
    internal static void SetPoiSearchClient(IPoiSearchClient client) => _poiClient = client;

    private static MapsSearchClient Client =>
        _client ?? throw new InvalidOperationException(
            "MapsSearchHandler is not configured. Call MapsSearchTools.Create(config) or " +
            "builder.UseAzureMapsSearch(config) before invoking any tool.");

    /// <summary>
    /// Geocodes <paramref name="location"/> to resolve coordinates, then searches for
    /// <paramref name="suggestedLocationTypes"/> (e.g. "coffee shop", "hospital") near
    /// that resolved location using the Azure Maps Geocoding API.
    /// </summary>
    /// <param name="suggestedLocationTypes">
    /// Type of point-of-interest to search for (e.g. "coffee shop", "pharmacy").
    /// </param>
    /// <param name="location">
    /// Free-text location used as the search centre (e.g. "Seattle, WA" or a full address).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of matching <see cref="Address"/> results, capped at
    /// <see cref="MapsSearchConfig.MaxResults"/>, or <see langword="null"/> when nothing
    /// is found.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="suggestedLocationTypes"/> or <paramref name="location"/> is blank.
    /// </exception>
    public static async Task<List<Address>?> SearchSuggestionAsync(
        string suggestedLocationTypes,
        string location,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(suggestedLocationTypes))
            throw new ArgumentException("suggestedLocationTypes must be a non-empty string.", nameof(suggestedLocationTypes));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("location must be a non-empty string.", nameof(location));

        // Step 1 — resolve the location to coordinates using the Geocoding API.
        Response<GeocodingResponse> locationResult =
            await Client.GetGeocodingAsync(location, cancellationToken: cancellationToken);

        if (locationResult.Value?.Features is not { Count: > 0 })
            return null;

        FeaturesItem locationFeature = locationResult.Value.Features[0];
        var coordinates = locationFeature.Geometry?.Coordinates;

        if (coordinates is null)
            return null;

        double lat = coordinates.Value.Latitude;
        double lon = coordinates.Value.Longitude;

        // Step 2 — use the Azure Maps Fuzzy Search REST API to find actual POIs by
        // category near the resolved coordinates.  The Geocoding API is an address
        // resolver and does NOT understand category queries (e.g. "coffee shop"), so
        // using it for Step 2 returns geographic results rather than points-of-interest.
        return await _poiClient.SearchAsync(
            query            : suggestedLocationTypes,
            lat              : lat,
            lon              : lon,
            maxResults       : _config.MaxResults,
            fallbackLocation : location,
            cancellationToken: cancellationToken);
    }
}
