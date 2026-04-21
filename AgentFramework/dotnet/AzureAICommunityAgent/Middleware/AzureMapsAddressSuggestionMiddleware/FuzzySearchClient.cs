using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

/// <summary>
/// Thin wrapper around the Azure Maps Fuzzy Search REST API
/// (<c>GET /search/fuzzy/json</c>) for POI / category queries near coordinates.
/// </summary>
/// <remarks>
/// The <c>Azure.Maps.Search</c> v2 beta SDK dropped the fuzzy-search typed client,
/// so this class calls the REST endpoint directly via <see cref="HttpClient"/>.
/// Replace this class with an SDK-backed implementation once a stable POI search
/// method is available, without changing <see cref="MapsSearchHandler"/>.
/// </remarks>
internal sealed class FuzzySearchClient : IPoiSearchClient
{
    private readonly HttpClient _http;
    private readonly string     _subscriptionKey;

    internal FuzzySearchClient(string subscriptionKey, HttpClient? httpClient = null)
    {
        _subscriptionKey = subscriptionKey;
        _http            = httpClient ?? new HttpClient();
    }

    /// <inheritdoc/>
    public async Task<List<Address>?> SearchAsync(
        string   query,
        double   lat,
        double   lon,
        int      maxResults,
        string   fallbackLocation,
        CancellationToken cancellationToken = default)
    {
        // Use InvariantCulture for lat/lon to guarantee a dot decimal separator
        // regardless of the host machine's regional settings (e.g. fr-FR uses commas,
        // which produces an invalid URL parameter and a 400 from the Maps API).
        string latStr = lat.ToString("G", CultureInfo.InvariantCulture);
        string lonStr = lon.ToString("G", CultureInfo.InvariantCulture);

        string url =
            $"https://atlas.microsoft.com/search/fuzzy/json" +
            $"?api-version=1.0" +
            $"&query={Uri.EscapeDataString(query)}" +
            $"&lat={latStr}&lon={lonStr}" +
            $"&limit={maxResults}" +
            $"&subscription-key={_subscriptionKey}";

        using var httpResponse = await _http.GetAsync(url, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
        var fuzzyResult = await JsonSerializer.DeserializeAsync<FuzzySearchResponse>(
            stream, cancellationToken: cancellationToken);

        if (fuzzyResult?.Results is not { Count: > 0 })
            return null;

        var addresses = new List<Address>();

        foreach (var result in fuzzyResult.Results)
        {
            var pos  = result.Position;
            var addr = result.Address;

            addresses.Add(new Address
            {
                Name          = result.Poi?.Name ?? addr?.FreeformAddress ?? fallbackLocation,
                Latitude      = pos?.Lat ?? lat,
                Longitude     = pos?.Lon ?? lon,
                Location      = addr?.FreeformAddress ?? fallbackLocation,
                StreetAddress = addr?.StreetNameAndNumber,
                StreetName    = addr?.StreetName,
                StreetNumber  = addr?.StreetNumber,
                City          = addr?.LocalName ?? addr?.Municipality,
                State         = addr?.CountrySubdivisionCode ?? addr?.CountrySubdivision,
                Country       = addr?.Country,
                PostalCode    = addr?.PostalCode,
                Neighborhood  = addr?.MunicipalitySubdivision,
                Category      = result.Poi?.Categories?.FirstOrDefault() ?? result.Type,
            });
        }

        return addresses.Count > 0 ? addresses : null;
    }
}

// ── Minimal response models for the Azure Maps Fuzzy Search REST API ─────────

internal sealed class FuzzySearchResponse
{
    [JsonPropertyName("results")]
    public List<FuzzyResult>? Results { get; set; }
}

internal sealed class FuzzyResult
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("poi")]
    public PoiInfo? Poi { get; set; }

    [JsonPropertyName("address")]
    public FuzzyAddress? Address { get; set; }

    [JsonPropertyName("position")]
    public LatLon? Position { get; set; }
}

internal sealed class PoiInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }
}

internal sealed class FuzzyAddress
{
    [JsonPropertyName("streetNumber")]
    public string? StreetNumber { get; set; }

    [JsonPropertyName("streetName")]
    public string? StreetName { get; set; }

    [JsonPropertyName("streetNameAndNumber")]
    public string? StreetNameAndNumber { get; set; }

    [JsonPropertyName("municipality")]
    public string? Municipality { get; set; }

    [JsonPropertyName("municipalitySubdivision")]
    public string? MunicipalitySubdivision { get; set; }

    [JsonPropertyName("localName")]
    public string? LocalName { get; set; }

    [JsonPropertyName("countrySubdivision")]
    public string? CountrySubdivision { get; set; }

    [JsonPropertyName("countrySubdivisionCode")]
    public string? CountrySubdivisionCode { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("freeformAddress")]
    public string? FreeformAddress { get; set; }
}

internal sealed class LatLon
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}
