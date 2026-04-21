using System.Text.Json;
using System.Text.RegularExpressions;

namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

/// <summary>
/// Represents a resolved address with optional geographic coordinates.
/// </summary>
public sealed class Address
{
    /// <summary>Display name or formatted address of the location.</summary>
    public string? Name { get; set; }

    /// <summary>Longitude of the location.</summary>
    public double Longitude { get; set; }

    /// <summary>Latitude of the location.</summary>
    public double Latitude { get; set; }

    /// <summary>Formatted address string returned by the geocoding API.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Street-level address line (house number + street name).</summary>
    public string? StreetAddress { get; set; }

    /// <summary>Street name only (without house number).</summary>
    public string? StreetName { get; set; }

    /// <summary>Street number (house / building number).</summary>
    public string? StreetNumber { get; set; }

    /// <summary>City or town name.</summary>
    public string? City { get; set; }

    /// <summary>State or province name.</summary>
    public string? State { get; set; }

    /// <summary>Country name.</summary>
    public string? Country { get; set; }

    /// <summary>Postal / ZIP code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Neighbourhood, borough, or sub-locality.</summary>
    public string? Neighborhood { get; set; }

    /// <summary>POI category returned by the geocoding API (e.g. "Restaurant", "Hospital").</summary>
    public string? Category { get; set; }

    /// <summary>Confidence score of the geocoding match (High / Medium / Low).</summary>
    public string? Confidence { get; set; }

    /// <summary>
    /// Unique feature ID returned by the geocoding API.
    /// Can be used for follow-up queries or map rendering.
    /// </summary>
    public string? FeatureId { get; set; }

    /// <summary>
    /// Bounding box of the feature [West, South, East, North] in EPSG:3857.
    /// Useful for map viewport fitting and area queries.
    /// </summary>
    public double[]? BoundingBox { get; set; }

    /// <summary>
    /// Match quality codes indicating how well the result matches the query
    /// (e.g. "Good", "Ambiguous", "UpHierarchy").
    /// </summary>
    public IReadOnlyList<string>? MatchCodes { get; set; }

    /// <summary>
    /// Geocode points with distinct roles (Route vs Display) and the calculation
    /// method used to derive each point.
    /// </summary>
    public IReadOnlyList<GeocodePoint>? GeocodePoints { get; set; }

    public override string ToString() => ToString(AddressFieldProfiles.Full);

    /// <summary>
    /// Returns a string containing only the fields indicated by <paramref name="fields"/>.
    /// Use one of the presets in <see cref="AddressFieldProfiles"/> or combine
    /// <see cref="AddressFieldOptions"/> flags for a custom layout.
    /// </summary>
    public string ToString(AddressFieldOptions fields)
    {
        bool Has(AddressFieldOptions f) => (fields & f) != 0;

        var parts = new List<string>(18);

        if (Has(AddressFieldOptions.Name))          parts.Add($"Name: {Name}");
        if (Has(AddressFieldOptions.Location))      parts.Add($"Location: {Location}");
        if (Has(AddressFieldOptions.Latitude))      parts.Add($"Latitude: {Latitude}");
        if (Has(AddressFieldOptions.Longitude))     parts.Add($"Longitude: {Longitude}");
        if (Has(AddressFieldOptions.StreetAddress)) parts.Add($"StreetAddress: {StreetAddress}");
        if (Has(AddressFieldOptions.StreetName))    parts.Add($"StreetName: {StreetName}");
        if (Has(AddressFieldOptions.StreetNumber))  parts.Add($"StreetNumber: {StreetNumber}");
        if (Has(AddressFieldOptions.City))          parts.Add($"City: {City}");
        if (Has(AddressFieldOptions.State))         parts.Add($"State: {State}");
        if (Has(AddressFieldOptions.Country))       parts.Add($"Country: {Country}");
        if (Has(AddressFieldOptions.PostalCode))    parts.Add($"PostalCode: {PostalCode}");
        if (Has(AddressFieldOptions.Neighborhood))  parts.Add($"Neighborhood: {Neighborhood}");
        if (Has(AddressFieldOptions.Category))      parts.Add($"Category: {Category}");
        if (Has(AddressFieldOptions.Confidence))    parts.Add($"Confidence: {Confidence}");
        if (Has(AddressFieldOptions.FeatureId))     parts.Add($"FeatureId: {FeatureId}");
        if (Has(AddressFieldOptions.MatchCodes))
        {
            var mc = MatchCodes is { Count: > 0 } ? string.Join(", ", MatchCodes) : null;
            parts.Add($"MatchCodes: {mc}");
        }
        if (Has(AddressFieldOptions.BoundingBox))
        {
            var bb = BoundingBox is { Length: > 0 } ? string.Join(", ", BoundingBox) : null;
            parts.Add($"BoundingBox: [{bb}]");
        }
        if (Has(AddressFieldOptions.GeocodePoints))
        {
            var gp = GeocodePoints is { Count: > 0 }
                ? string.Join(" | ", GeocodePoints.Select(p => p.ToString()))
                : null;
            parts.Add($"GeocodePoints: [{gp}]");
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Serializes this address to a JSON string.
    /// When <paramref name="includeCoordinates"/> is <see langword="false"/>, only
    /// <see cref="Location"/> is serialized.
    /// </summary>
    public string Serialize(bool includeCoordinates)
    {
        var obj = includeCoordinates ? (object)this : Location;
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        return Regex.Unescape(json);
    }

    /// <summary>Deserializes an <see cref="Address"/> from a JSON string.</summary>
    public static Address? Deserialize(string jsonString) =>
        JsonSerializer.Deserialize<Address>(jsonString);
}

/// <summary>
/// A single geocode point with its calculation method and intended usage types.
/// </summary>
public sealed class GeocodePoint
{
    /// <summary>How the point was calculated (e.g. "Interpolation", "Rooftop").</summary>
    public string? CalculationMethod { get; set; }

    /// <summary>Intended use of this point: "Route" (navigation) or "Display" (map pin).</summary>
    public IReadOnlyList<string>? UsageTypes { get; set; }

    /// <summary>Longitude of this geocode point.</summary>
    public double Longitude { get; set; }

    /// <summary>Latitude of this geocode point.</summary>
    public double Latitude { get; set; }

    public override string ToString()
    {
        var usages = UsageTypes is { Count: > 0 } ? string.Join(", ", UsageTypes) : null;
        return $"({CalculationMethod} [{usages}] Lat={Latitude}, Lon={Longitude})";
    }
}
