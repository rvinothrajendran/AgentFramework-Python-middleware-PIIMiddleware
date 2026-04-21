using AzureAICommunity.Agent.Middleware.YouTubeMiddleware;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace YouTubeSearch;

internal class Program
{
    const string Endpoint = "http://localhost:11434/";
    const string Model = "llama3.2";

    static async Task Main(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("Set YOUTUBE_API_KEY environment variable first.");
            return;
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(Endpoint),
            Timeout = TimeSpan.FromMinutes(5)
        };

        YouTubeConfig youTubeConfig = new YouTubeConfig
        {
            ApiKey = apiKey,
            ChannelId = Environment.GetEnvironmentVariable("YOUTUBE_CHANNEL_ID") ?? string.Empty,
            MaxResults = 25,
            DefaultCount = 5
        };

        var tools = YouTubeTools.Create(youTubeConfig);

        IChatClient ollamaClient = new OllamaApiClient(httpClient, Model);

        AIAgent originalAgent = new ChatClientAgent(ollamaClient,
            instructions: """
                          You are a file-search assistant with access to tools that can search the file system and YouTube.
                          Always choose and invoke the correct tool based on the user's request.
                          If the user asks for videos (or video references), you must call the YouTube search tool.
                          When returning video results, always include the full direct YouTube URL (https://www.youtube.com/watch?v=...).
                          Do not return video titles without URLs.
                          If multiple videos are requested, return a numbered list and include one URL per item.
                          """,
            tools: tools);

        AIAgent agent = new AIAgentBuilder(originalAgent)
            .UseYouTubeSearch()
            .Build();

      
        var prompt = args.Length > 0
            ? string.Join(' ', args)
            : "GenAI concept and some best videos";

        var response = await agent.RunAsync(prompt);

        Console.WriteLine(response);

        Console.Read();
    }
}
