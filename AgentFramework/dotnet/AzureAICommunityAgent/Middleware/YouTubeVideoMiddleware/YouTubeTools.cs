using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.YouTube;

/// <summary>
/// Entry point for integrating YouTube search into an AI agent pipeline.
/// <list type="bullet">
///   <item><description><see cref="Create"/> builds the <see cref="Microsoft.Extensions.AI.AITool"/> array from a <see cref="YouTubeConfig"/>.</description></item>
///   <item><description><see cref="UseYouTubeSearch"/> registers a middleware handler on an <see cref="Microsoft.Agents.AI.AIAgentBuilder"/> that intercepts tool invocations and routes <c>SearchVideos</c> calls to <see cref="YouTubeFetch"/>.</description></item>
/// </list>
/// </summary>
public static class YouTubeTools
{
    private static YouTubeConfig _config = new();

    /// <summary>
    /// Creates the array of <see cref="Microsoft.Extensions.AI.AITool"/> objects from the supplied configuration.
    /// Throws if <paramref name="config"/> is null or its <see cref="YouTubeConfig.ApiKey"/> is empty.
    /// </summary>
    /// <param name="config">Configuration supplying API credentials and search defaults.</param>
    /// <returns>An array of AI tools ready to be registered with an agent.</returns>
    public static AITool[] Create(YouTubeConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
            throw new ArgumentException("ApiKey must be set in YouTubeConfig.", nameof(config));

        return YouTubeToolFactory.CreateTools(_config).Cast<AITool>().ToArray();
    }

    /// <summary>
    /// Extension method that registers the YouTube search middleware on the given <paramref name="builder"/>.
    /// When a tool invocation with name <c>SearchVideos</c> arrives, the middleware handles it
    /// directly; all other invocations are forwarded to the next handler in the pipeline.
    /// </summary>
    /// <param name="builder">The agent builder to extend.</param>
    /// <returns>The same <paramref name="builder"/> instance to allow method chaining.</returns>
    public static AIAgentBuilder UseYouTubeSearch(this AIAgentBuilder builder)
        => builder.Use(Invoke);

    private static async ValueTask<object?> Invoke(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        return context.Function.Name switch
        {
            YouTubeToolNames.SearchVideos => await HandleSearch(context.Arguments, cancellationToken),
            _ => await next(context, cancellationToken)
        };
    }

    private static async Task<string> HandleSearch(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var query = GetString(args, "query", string.Empty);
        var count = GetInt(args, "count", _config.DefaultCount);
        var offset = GetInt(args, "offset", 0);

        var result = await YouTubeFetch.SearchVideosAsync(query, count, offset, _config, cancellationToken);

        string resultString = string.Empty;
        
        if (result?.Count > 0)
        {
            resultString = result.Aggregate(resultString, (current, item) => current + (item + "\n"));
        }

        return resultString;
        
    }

    private static string GetString(IReadOnlyDictionary<string, object?> args, string key, string defaultValue)
    {
        if (!args.TryGetValue(key, out var value))
            return defaultValue;

        return value?.ToString() ?? defaultValue;
    }

    private static int GetInt(IReadOnlyDictionary<string, object?> args, string key, int defaultValue)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
            return defaultValue;

        return value switch
        {
            int i => i,
            long l when l <= int.MaxValue && l >= int.MinValue => (int)l,
            _ when int.TryParse(value.ToString(), out var parsed) => parsed,
            _ => defaultValue
        };
    }
}

