namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

// ── Field selector ────────────────────────────────────────────────────────────

/// <summary>
/// Bit-flags that control which <see cref="Address"/> fields are included in the
/// string returned to the LLM.  Combine flags freely for a custom field set, or
/// use one of the pre-built presets in <see cref="AddressFieldProfiles"/>.
/// </summary>
[Flags]
public enum AddressFieldOptions : long
{
    None          = 0,

    // Core identity
    Name          = 1L << 0,
    Location      = 1L << 1,

    // Coordinates
    Latitude      = 1L << 2,
    Longitude     = 1L << 3,

    // Street
    StreetAddress = 1L << 4,
    StreetName    = 1L << 5,
    StreetNumber  = 1L << 6,

    // Administrative
    City          = 1L << 7,
    State         = 1L << 8,
    Country       = 1L << 9,
    PostalCode    = 1L << 10,
    Neighborhood  = 1L << 11,

    // POI metadata
    Category      = 1L << 12,
    Confidence    = 1L << 13,

    // Geocoding metadata
    FeatureId     = 1L << 14,
    MatchCodes    = 1L << 15,
    BoundingBox   = 1L << 16,
    GeocodePoints = 1L << 17,
}

// ── Profile selector ──────────────────────────────────────────────────────────

/// <summary>
/// Selects a pre-built or custom field profile for <see cref="MapsSearchConfig"/>.
/// </summary>
public enum AddressFieldProfile
{
    /// <summary>
    /// All fields — identical to the previous default behaviour.
    /// </summary>
    Full,

    /// <summary>
    /// Human-readable address only: name, formatted location, city, state,
    /// country and postal code.  Smallest payload; ideal for display-only UIs.
    /// </summary>
    Basic,

    /// <summary>
    /// Everything needed to route to a place: coordinates, full street detail,
    /// administrative levels and geocode route-points.
    /// </summary>
    Navigation,

    /// <summary>
    /// Everything needed to render a map pin or card: coordinates, bounding box,
    /// name, city, state, country, neighbourhood and category.
    /// </summary>
    Display,

    /// <summary>
    /// Use the <see cref="MapsSearchConfig.CustomFields"/> property to specify
    /// exactly which fields to include.
    /// </summary>
    Custom,
}

// ── Pre-built presets ─────────────────────────────────────────────────────────

/// <summary>
/// Returns the <see cref="AddressFieldOptions"/> flag-set that corresponds to each
/// <see cref="AddressFieldProfile"/> pre-built profile.
/// </summary>
public static class AddressFieldProfiles
{
    /// <summary>All available fields.</summary>
    public static readonly AddressFieldOptions Full =
        (AddressFieldOptions)((1L << 18) - 1);   // every bit up to GeocodePoints

    /// <summary>Name, Location, City, State, Country, PostalCode.</summary>
    public static readonly AddressFieldOptions Basic =
        AddressFieldOptions.Name     |
        AddressFieldOptions.Location |
        AddressFieldOptions.City     |
        AddressFieldOptions.State    |
        AddressFieldOptions.Country  |
        AddressFieldOptions.PostalCode;

    /// <summary>
    /// Name, Location, Lat/Lon, StreetAddress, StreetName, StreetNumber,
    /// City, State, Country, PostalCode, GeocodePoints.
    /// </summary>
    public static readonly AddressFieldOptions Navigation =
        AddressFieldOptions.Name          |
        AddressFieldOptions.Location      |
        AddressFieldOptions.Latitude      |
        AddressFieldOptions.Longitude     |
        AddressFieldOptions.StreetAddress |
        AddressFieldOptions.StreetName    |
        AddressFieldOptions.StreetNumber  |
        AddressFieldOptions.City          |
        AddressFieldOptions.State         |
        AddressFieldOptions.Country       |
        AddressFieldOptions.PostalCode    |
        AddressFieldOptions.GeocodePoints;

    /// <summary>
    /// Name, Location, Lat/Lon, BoundingBox, City, State, Country,
    /// Neighborhood, Category.
    /// </summary>
    public static readonly AddressFieldOptions Display =
        AddressFieldOptions.Name         |
        AddressFieldOptions.Location     |
        AddressFieldOptions.Latitude     |
        AddressFieldOptions.Longitude    |
        AddressFieldOptions.BoundingBox  |
        AddressFieldOptions.City         |
        AddressFieldOptions.State        |
        AddressFieldOptions.Country      |
        AddressFieldOptions.Neighborhood |
        AddressFieldOptions.Category;

    /// <summary>
    /// Resolves a <see cref="AddressFieldProfile"/> to its <see cref="AddressFieldOptions"/>
    /// flag-set.  When <paramref name="profile"/> is <see cref="AddressFieldProfile.Custom"/>,
    /// <paramref name="customFields"/> is returned as-is.
    /// </summary>
    public static AddressFieldOptions Resolve(
        AddressFieldProfile  profile,
        AddressFieldOptions  customFields = AddressFieldOptions.None)
        => profile switch
        {
            AddressFieldProfile.Full       => Full,
            AddressFieldProfile.Basic      => Basic,
            AddressFieldProfile.Navigation => Navigation,
            AddressFieldProfile.Display    => Display,
            AddressFieldProfile.Custom     => customFields,
            _                              => Full
        };
}
