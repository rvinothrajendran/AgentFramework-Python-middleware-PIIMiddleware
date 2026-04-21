using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.YouTube;

/// <summary>
/// Creates <see cref="Microsoft.Extensions.AI.AIFunction"/> instances that expose YouTube search
/// capabilities as callable AI tools.
/// Use <see cref="CreateTools"/> to obtain the tool set and register it with an AI agent builder.
/// </summary>
public static class YouTubeToolFactory
{
    /// <summary>
    /// Builds and returns the set of AI functions backed by the supplied <paramref name="config"/>.
    /// Currently yields a single <c>SearchVideos</c> function that accepts a query, count, and offset.
    /// </summary>
    /// <param name="config">YouTube configuration used by the underlying search logic.</param>
    /// <returns>An enumerable of <see cref="Microsoft.Extensions.AI.AIFunction"/> objects ready for registration.</returns>
    public static IEnumerable<AIFunction> CreateTools(YouTubeConfig config)
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        yield return AIFunctionFactory.Create(
            async (
                [Description("Natural language search query for YouTube videos.")] string query,
                [Description("Number of videos to return. Defaults to config value when <= 0.")] int count,
                [Description("Number of matched videos to skip before returning results.")] int offset
            ) => await YouTubeFetch.SearchVideosAsync(query, count, offset, config),
            new AIFunctionFactoryOptions
            {
                Name = YouTubeToolNames.SearchVideos,
                Description = "Search YouTube videos and return matching watch URLs."
            });
    }
}

