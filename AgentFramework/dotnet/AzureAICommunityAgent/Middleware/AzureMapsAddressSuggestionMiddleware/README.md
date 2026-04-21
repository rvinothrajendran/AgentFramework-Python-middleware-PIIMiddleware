<div align="center">

# 🗺️ AzureAICommunity – Azure Maps Address Suggestion Middleware (.NET)

Find points of interest anywhere in the world directly from your AI agent pipeline using **Azure Maps**.

[![NuGet Version](https://img.shields.io/nuget/v/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware)](https://www.nuget.org/packages/AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware/)
[![License](https://img.shields.io/github/license/rvinothrajendran/AgentFramework)](https://github.com/rvinothrajendran/AgentFramework/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![GitHub Repo](https://img.shields.io/badge/GitHub-AgentFramework-181717?logo=github)](https://github.com/rvinothrajendran/AgentFramework)
[![GitHub Follow](https://img.shields.io/github/followers/rvinothrajendran?label=Follow%20%40rvinothrajendran&style=social)](https://github.com/rvinothrajendran)
[![YouTube Channel](https://img.shields.io/badge/YouTube-VinothRajendran-FF0000?logo=youtube&logoColor=white)](https://www.youtube.com/@VinothRajendran)
[![YouTube Subscribers](https://img.shields.io/youtube/channel/subscribers/UCQf_yRJpsfyEiWWpt1MZ6vA?label=Subscribers&style=social)](https://www.youtube.com/@VinothRajendran)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-rvinothrajendran-0A66C2?logo=linkedin&logoColor=white)](https://www.linkedin.com/in/rvinothrajendran/)

[Getting Started](#-installation) · [Configuration](#️-configuration) · [Usage](#-usage) · [Field Profiles](#-field-profiles) · [How It Works](#️-how-it-works) · [Type Reference](#-type-reference) · [Contributing](#-contributing)

</div>

---

## Overview

`AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware` is a plug-and-play location search layer for AI agent pipelines built on `Microsoft.Agents.AI` and `Microsoft.Extensions.AI`. It exposes a `SearchSuggestionAsync` AI tool that geocodes a location to coordinates (via the Azure Maps Geocoding API) and then finds real points of interest nearby (via the Azure Maps Fuzzy Search API) — returning rich address data with only the fields your scenario actually needs.

---

## ✨ Features

| | Feature |
|---|---|
| 📍 | **Real POI search** — finds actual businesses and places, not just geographic names |
| 🎛️ | **Field profiles** — `Basic`, `Navigation`, `Display`, `Full`, or `Custom` to control payload size |
| 🤖 | **AI tool integration** — registers as an `AITool` callable by the LLM automatically |
| 🔌 | **Drop-in middleware** — one `.UseAzureMapsSearch()` call wires everything into the pipeline |
| 🌍 | **Any location format** — city name, landmark, full address, or coordinates |
| 🛡️ | **Culture-safe URLs** — coordinates always formatted with invariant decimal separator |

---

## 📦 Installation

```bash
dotnet add package AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware
```

---

## 🚀 Quick Start

```csharp
using AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

var mapsConfig = new MapsSearchConfig
{
    AzureMapsKey = Environment.GetEnvironmentVariable("AZURE_MAPS_KEY")!,
    MaxResults   = 5,
    Profile      = AddressFieldProfile.Basic,
};

var tools = MapsSearchTools.Create(mapsConfig);

IChatClient baseClient = new OllamaApiClient("http://localhost:11434/", "llama3.2");

AIAgent originalAgent = new ChatClientAgent(baseClient,
    instructions: """
        You are a helpful location assistant powered by Azure Maps.
        Always call the Azure Maps tool whenever the user asks to find any type of place.
        Never answer from memory — always rely on the tool response for location data.
        List every result returned by the tool without omitting any entry.
        """,
    tools: tools);

AIAgent agent = new AIAgentBuilder(originalAgent)
    .UseAzureMapsSearch(mapsConfig)
    .Build();

var response = await agent.RunAsync("Find coffee shops near Chennai and list every address.");
Console.WriteLine(response);
```

---

## ⚙️ Configuration

All settings are provided through a `MapsSearchConfig` instance:

| Property | Type | Default | Description |
|---|---|---|---|
| `AzureMapsKey` | `string` | *(required)* | Azure Maps subscription key for shared-key authentication |
| `MaxResults` | `int` | `10` | Maximum number of POI results returned per search call |
| `Profile` | `AddressFieldProfile` | `Full` | Controls which address fields are returned to the LLM |
| `CustomFields` | `AddressFieldOptions` | `None` | Exact fields to include when `Profile` is `Custom` |

```csharp
var mapsConfig = new MapsSearchConfig
{
    AzureMapsKey = Environment.GetEnvironmentVariable("AZURE_MAPS_KEY")!,
    MaxResults   = 10,
    Profile      = AddressFieldProfile.Navigation,
};
```

---

## 🧑‍💻 Usage

### Middleware Pipeline

Register the middleware on an `AIAgentBuilder` so the agent automatically intercepts and handles `SearchSuggestionAsync` tool calls:

```csharp
var tools = MapsSearchTools.Create(mapsConfig);

AIAgent agent = new AIAgentBuilder(
        new ChatClientAgent(baseClient, instructions: "...", tools: tools))
    .UseAzureMapsSearch(mapsConfig)
    .Build();

var response = await agent.RunAsync("Find hospitals near Austin, TX.");
Console.WriteLine(response);
```

### Custom Field Profile

Cherry-pick exactly the fields your scenario needs:

```csharp
var mapsConfig = new MapsSearchConfig
{
    AzureMapsKey = Environment.GetEnvironmentVariable("AZURE_MAPS_KEY")!,
    MaxResults   = 5,
    Profile      = AddressFieldProfile.Custom,
    CustomFields = AddressFieldOptions.Name
                 | AddressFieldOptions.City
                 | AddressFieldOptions.Country
                 | AddressFieldOptions.Latitude
                 | AddressFieldOptions.Longitude
                 | AddressFieldOptions.Category,
};
```

---

## 🎛️ Field Profiles

Five built-in profiles control exactly which address fields reach the LLM — keeping the payload small and relevant:

| Profile | Fields included | Best for |
|---|---|---|
| `Basic` | Name · Location · City · State · Country · PostalCode | Display-only UIs, minimal payload |
| `Navigation` | + Lat/Lon · StreetAddress · StreetName · StreetNumber · GeocodePoints | Turn-by-turn navigation |
| `Display` | + Lat/Lon · BoundingBox · Category · Neighborhood | Map pins and cards |
| `Full` | All available fields | Maximum LLM context |
| `Custom` | Exactly the `CustomFields` flags you specify | Any bespoke scenario |

### Available `AddressFieldOptions` Flags

```
Name · Location · Latitude · Longitude
StreetAddress · StreetName · StreetNumber
City · State · Country · PostalCode · Neighborhood
Category · Confidence · FeatureId
MatchCodes · BoundingBox · GeocodePoints
```

Combine any flags with `|`:

```csharp
CustomFields = AddressFieldOptions.Name | AddressFieldOptions.Latitude | AddressFieldOptions.Longitude
```

---

## 🔑 Getting an Azure Maps Key

1. Go to the [Azure Portal](https://portal.azure.com/).
2. Create an **Azure Maps Account** resource.
3. Navigate to **Authentication** and copy the **Primary Key**.
4. Store the key in an environment variable:

```powershell
# Windows PowerShell
$env:AZURE_MAPS_KEY = "YOUR_KEY_HERE"
```

```bash
# Linux / macOS
export AZURE_MAPS_KEY="YOUR_KEY_HERE"
```

> ⚠️ **Never commit your Azure Maps key to source control.**

---

## 📐 Type Reference

### `MapsSearchConfig`

| Property | Type | Description |
|---|---|---|
| `AzureMapsKey` | `string` | Azure Maps subscription key |
| `MaxResults` | `int` | Max POI results per call (default `10`) |
| `Profile` | `AddressFieldProfile` | Pre-built field profile |
| `CustomFields` | `AddressFieldOptions` | Flags used when `Profile = Custom` |

### `Address`

| Property | Type | Description |
|---|---|---|
| `Name` | `string?` | POI display name or formatted address |
| `Location` | `string` | Freeform address string from the API |
| `Latitude` / `Longitude` | `double` | Coordinates of the POI |
| `StreetAddress` | `string?` | House number + street name |
| `StreetName` / `StreetNumber` | `string?` | Individual street components |
| `City` | `string?` | Locality / city name |
| `State` | `string?` | State or province code |
| `Country` | `string?` | Country name |
| `PostalCode` | `string?` | Postal / ZIP code |
| `Neighborhood` | `string?` | Sub-locality or borough |
| `Category` | `string?` | POI category (e.g. `coffee shop`) |
| `Confidence` | `string?` | Geocode match confidence |
| `BoundingBox` | `double[]?` | `[West, South, East, North]` |
| `GeocodePoints` | `IReadOnlyList<GeocodePoint>?` | Route and display geocode points |

### `MapsSearchTools`

| Member | Description |
|---|---|
| `MapsSearchTools.Create(config)` | Creates the `AITool[]` array to pass to `ChatClientAgent` |
| `.UseAzureMapsSearch(config)` | Extension method — registers middleware on `AIAgentBuilder` |

---

## 🤝 Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to change before submitting a pull request.

📁 **Repository:** [https://github.com/rvinothrajendran/AgentFramework](https://github.com/rvinothrajendran/AgentFramework)

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 👤 Author

Built and maintained by **Vinoth Rajendran**.

- 🐙 GitHub: [github.com/rvinothrajendran](https://github.com/rvinothrajendran) — _follow for more projects!_
- 📺 YouTube: [youtube.com/@VinothRajendran](https://www.youtube.com/@VinothRajendran) — _subscribe for tutorials and demos!_
- 💼 LinkedIn: [linkedin.com/in/rvinothrajendran](https://www.linkedin.com/in/rvinothrajendran/) — _let's connect!_

---

## 📄 License

MIT
