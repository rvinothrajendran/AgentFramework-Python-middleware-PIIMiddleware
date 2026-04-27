using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using AzureAICommunityAgent.Middleware.ToolLimiting;

namespace ToolLimit
{
    internal class Program
    {
        const string Endpoint = "http://localhost:11434/";
        const string Model = "llama3.2";
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(Endpoint),
                Timeout = TimeSpan.FromMinutes(5)
            };

            IChatClient ollamaClient = new OllamaApiClient(httpClient, Model);

            var tool = AIFunctionFactory.Create(GetWeather);
         
            var client = ollamaClient
                .AsBuilder()
                .UseToolLimit(new ToolLimits
                {
                    GlobalMax = 5,
                    PerToolMax = new Dictionary<string, int> { ["GetWeather"] = 3 }
                })
                .Build();

            AIAgent originalAgent = new ChatClientAgent(client,
                instructions: """
                              Helpful Assistant with access to a single tool that gets the weather for a given location.
                              """,
                tools: [tool]);

            var response = await originalAgent.RunAsync("What is the weather like in Amsterdam ?");

            Console.WriteLine(response.Text);

            var tracker = client.GetService<IToolLimitTracker>();
            if (tracker is not null)
            {
                var usage = tracker.GetCurrentUsage();
                Console.WriteLine($"Total allowed calls: {usage.TotalCalls} / {usage.GlobalLimit}");
                Console.WriteLine("Per-tool usage (attempted / allowed / limit):");
                foreach (var (toolName, attempted) in usage.PerTool)
                {
                    usage.PerToolAllowed.TryGetValue(toolName, out var allowed);
                    usage.PerToolLimits.TryGetValue(toolName, out var perMax);
                    var limitText = perMax > 0 ? $" / {perMax}" : string.Empty;
                    Console.WriteLine($"  {toolName}: attempted={attempted}  allowed={allowed}{limitText}");
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }

        [Description("Get the weather for a given location.")]
        static string GetWeather([Description("The location to get the weather for.")] string location)
            => $"The weather in {location} is cloudy with a high of 15°C.";
    }
}
