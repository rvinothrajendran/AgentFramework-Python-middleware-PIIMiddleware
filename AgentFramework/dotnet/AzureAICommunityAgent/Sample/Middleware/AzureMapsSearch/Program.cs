using AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace AzureMapsSearch;

internal class Program
{
    const string Endpoint = "http://localhost:11434/";
    const string Model    = "llama3.2";

    // Set your Azure Maps subscription key here or via environment variable AZURE_MAPS_KEY
    static readonly string AzureMapsKey =
        Environment.GetEnvironmentVariable("AZURE_MAPS_KEY") ?? "";

    static async Task Main(string[] args)
    {
        ConsoleUI.PrintBanner();

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(Endpoint),
            Timeout     = TimeSpan.FromMinutes(5)
        };

        IChatClient baseClient = new OllamaApiClient(httpClient, Model);

        // ── Demo 1 : Basic profile ────────────────────────────────────────────
        // Only name, formatted location, city, state, country and postal code
        // are sent to the LLM — smallest possible payload.
        await RunDemoAsync(baseClient,
            profile      : AddressFieldProfile.Basic,
            title        : "Demo 1 – Basic profile  (Name · Location · City · State · Country · PostalCode)",
            prompt       : "Find coffee shops near Chennai and list every address you find.");

        
        
        // ── Demo 2 : Navigation profile ───────────────────────────────────────
        // Includes coordinates, full street breakdown and geocode route-points —
        // everything needed to navigate to a place.
        await RunDemoAsync(baseClient,
            profile      : AddressFieldProfile.Navigation,
            title        : "Demo 2 – Navigation profile  (+ Lat/Lon · Street · GeocodePoints)",
            prompt       : "Find hospitals near Austin, TX and list every address you find.");

        // ── Demo 3 : Display profile ──────────────────────────────────────────
        // Includes coordinates, bounding box, category and neighbourhood —
        // ideal for rendering a map card or pin.
        await RunDemoAsync(baseClient,
            profile      : AddressFieldProfile.Display,
            title        : "Demo 3 – Display profile  (+ Lat/Lon · BoundingBox · Category · Neighborhood)",
            prompt       : "Find restaurants near London, UK and list every address you find.");

        // ── Demo 4 : Full profile ─────────────────────────────────────────────
        // Every field the geocoding API returns — maximum LLM context.
        await RunDemoAsync(baseClient,
            profile      : AddressFieldProfile.Full,
            title        : "Demo 4 – Full profile  (all fields)",
            prompt       : "Find pharmacies near New York City and list every address you find.");

        // ── Demo 5 : Custom profile ───────────────────────────────────────────
        // Cherry-pick exactly the fields that matter for your scenario.
        // Here we want just enough to show a compact summary card:
        //   Name · City · Country · Latitude · Longitude · Category · Confidence
        await RunDemoAsync(baseClient,
            profile      : AddressFieldProfile.Custom,
            customFields : AddressFieldOptions.Name
                         | AddressFieldOptions.City
                         | AddressFieldOptions.Country
                         | AddressFieldOptions.Latitude
                         | AddressFieldOptions.Longitude
                         | AddressFieldOptions.Category
                         | AddressFieldOptions.Confidence,
            title        : "Demo 5 – Custom profile  (Name · City · Country · Lat · Lon · Category · Confidence)",
            prompt       : "Find ATMs near Eiffel Tower, Paris and list every result you find.");

        Console.WriteLine();
        ConsoleUI.Println(ConsoleColor.Cyan, "Done.");
    }

    // ── Helper — creates a fresh agent per demo so each uses its own profile ──

    private static async Task RunDemoAsync(
        IChatClient         baseClient,
        AddressFieldProfile profile,
        string              title,
        string              prompt,
        AddressFieldOptions customFields = AddressFieldOptions.None)
    {
        var mapsConfig = new MapsSearchConfig

        {
            AzureMapsKey = AzureMapsKey,
            MaxResults   = 5,
            Profile      = profile,
            CustomFields = customFields,          // only used when Profile == Custom
        };

        var tools = MapsSearchTools.Create(mapsConfig);

        AIAgent originalAgent = new ChatClientAgent(baseClient,
            instructions: """
                You are a helpful location assistant powered by Azure Maps.
                Your job is to help users discover places and points of interest anywhere in the world.

                Rules you must always follow:
                - Always call the Azure Maps address suggestion tool whenever the user asks to find, locate, or explore any type of place or point of interest.
                - Never answer from memory. Always rely on the tool response for location data.
                - List every single result returned by the tool. Do not summarise, filter, merge, or omit any entry.
                - For each result, include every field exactly as returned by the tool — do not add or invent any extra information.
                - If the tool returns no results, clearly inform the user that no locations were found for their query.
                - Accept any location format from the user: city name, landmark, partial address, or coordinates.
                """,
            tools: tools);

        AIAgent agent = new AIAgentBuilder(originalAgent)
            .UseAzureMapsSearch(mapsConfig)
            .Build();

        ConsoleUI.PrintSection(title);
        ConsoleUI.Println(ConsoleColor.DarkGray, $"  > {prompt}");

        try
        {
            var response = await agent.RunAsync(prompt);
            ConsoleUI.Println(ConsoleColor.White, $"\n  LLM: {response}");
        }
        catch (Exception ex)
        {
            ConsoleUI.Println(ConsoleColor.Red, $"  ✗ {ex.Message}");
        }
    }
}

