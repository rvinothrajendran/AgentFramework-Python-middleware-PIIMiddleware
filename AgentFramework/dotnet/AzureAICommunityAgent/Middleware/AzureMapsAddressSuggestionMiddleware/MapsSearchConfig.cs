namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

/// <summary>
/// Runtime configuration for the Azure Maps address suggestion tools.
/// All properties have sensible defaults; override only what you need.
/// </summary>
/// <example>
/// Pre-built profile — only navigation fields returned to the LLM:
/// <code>
/// var tools = MapsSearchTools.Create(new MapsSearchConfig
/// {
///     AzureMapsKey = "&lt;subscription-key&gt;",
///     MaxResults   = 10,
///     Profile      = AddressFieldProfile.Navigation,
/// });
/// </code>
/// Custom profile — cherry-pick exactly the fields you want:
/// <code>
/// var tools = MapsSearchTools.Create(new MapsSearchConfig
/// {
///     AzureMapsKey  = "&lt;subscription-key&gt;",
///     Profile       = AddressFieldProfile.Custom,
///     CustomFields  = AddressFieldOptions.Name
///                   | AddressFieldOptions.City
///                   | AddressFieldOptions.Country
///                   | AddressFieldOptions.Latitude
///                   | AddressFieldOptions.Longitude,
/// });
/// </code>
/// </example>
public sealed class MapsSearchConfig
{
    /// <summary>
    /// Azure Maps subscription key (shared-key authentication).
    /// Required — must be set before calling any tool.
    /// </summary>
    public string AzureMapsKey { get; init; } = string.Empty;

    /// <summary>Maximum number of POI results returned per search call. Default: 10.</summary>
    public int MaxResults { get; init; } = 10;

    /// <summary>
    /// Controls which <see cref="Address"/> fields are included in the string
    /// returned to the LLM.
    /// <list type="bullet">
    ///   <item><see cref="AddressFieldProfile.Full"/> — all fields (default).</item>
    ///   <item><see cref="AddressFieldProfile.Basic"/> — name, location, city, state, country, postal code.</item>
    ///   <item><see cref="AddressFieldProfile.Navigation"/> — coordinates, full street detail, geocode route-points.</item>
    ///   <item><see cref="AddressFieldProfile.Display"/> — coordinates, bounding box, category, neighbourhood.</item>
    ///   <item><see cref="AddressFieldProfile.Custom"/> — use <see cref="CustomFields"/> to specify fields.</item>
    /// </list>
    /// </summary>
    public AddressFieldProfile Profile { get; init; } = AddressFieldProfile.Full;

    /// <summary>
    /// Exact fields to include when <see cref="Profile"/> is
    /// <see cref="AddressFieldProfile.Custom"/>.  Ignored for all other profiles.
    /// Combine flags with the bitwise-OR operator:
    /// <code>
    /// CustomFields = AddressFieldOptions.Name | AddressFieldOptions.City | AddressFieldOptions.Latitude
    /// </code>
    /// </summary>
    public AddressFieldOptions CustomFields { get; init; } = AddressFieldOptions.None;

    /// <summary>
    /// Resolves the active <see cref="AddressFieldOptions"/> from <see cref="Profile"/>
    /// and <see cref="CustomFields"/>.
    /// </summary>
    internal AddressFieldOptions ActiveFields =>
        AddressFieldProfiles.Resolve(Profile, CustomFields);
}
