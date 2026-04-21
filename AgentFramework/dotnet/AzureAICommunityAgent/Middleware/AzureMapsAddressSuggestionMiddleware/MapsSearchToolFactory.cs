using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

/// <summary>
/// Creates <see cref="AIFunction"/> tool instances for the Azure Maps address suggestion tool,
/// ready to be registered with a Microsoft.Extensions.AI chat pipeline.
/// </summary>
/// <example>
/// Register with a chat client:
/// <code>
/// var config = new MapsSearchConfig { AzureMapsKey = "&lt;key&gt;", MaxResults = 5 };
/// var tools  = MapsSearchToolFactory.CreateTools(config).ToList();
///
/// var options = new ChatOptions { Tools = tools };
/// var response = await client.GetResponseAsync(messages, options);
/// </code>
/// </example>
public static class MapsSearchToolFactory
{
    /// <summary>
    /// Returns the <c>SearchSuggestionAsync</c> <see cref="AIFunction"/> tool.
    /// </summary>
    /// <param name="config">
    ///   Configuration applied to every search call made through the returned tool.
    ///   When <see langword="null"/>, a default <see cref="MapsSearchConfig"/> is used
    ///   (requires <see cref="MapsSearchConfig.AzureMapsKey"/> to have been set already).
    /// </param>
    public static IEnumerable<AIFunction> CreateTools(MapsSearchConfig? config = null)
    {
        if (config is not null)
            MapsSearchHandler.Configure(config);

        var activeFields = (config ?? new MapsSearchConfig()).ActiveFields;

        async Task<string> SearchSuggestionTool(
            [Description("The type of place or point-of-interest to search for. " + "Must be a short category noun only — do NOT include any location or address here. " + "Examples: 'coffee shop', 'hospital', 'pharmacy', 'restaurant', 'atm'.")] 
            string suggestedLocationTypes,
            [Description("The geographic location to search near. " + "Must be a place name, city, address, or landmark only — do NOT include the place type here. " + "Examples: 'Seattle, WA', 'Chennai, India', '10 Downing Street London', 'Eiffel Tower'.")] 
            string location)
        {
            var addresses = await MapsSearchHandler.SearchSuggestionAsync(suggestedLocationTypes, location);

            if (addresses is not { Count: > 0 }) return "No results found.";

            return string.Join(Environment.NewLine, addresses.Select(a => a.ToString(activeFields)));
        }

        var aiFunctionFactoryOptions = new AIFunctionFactoryOptions
        {
            Name        = MapsSearchToolNames.SearchSuggestion,
            Description =
                "Finds points of interest of a given type near a specified location using Azure Maps. " +
                "Call this tool whenever the user wants to find, locate, or explore any type of place. " +
                "Pass ONLY the place category in 'suggestedLocationTypes' and ONLY the location in 'location' — never mix them. " +
                "Returns a list of matching places with full address and coordinates."
        };  
        
        yield return AIFunctionFactory.Create(SearchSuggestionTool,aiFunctionFactoryOptions);
    }


}
