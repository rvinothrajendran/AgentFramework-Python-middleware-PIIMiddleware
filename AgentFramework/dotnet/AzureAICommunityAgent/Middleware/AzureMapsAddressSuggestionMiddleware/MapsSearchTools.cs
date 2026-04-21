using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.AzureMapsAddressSuggestionMiddleware;

/// <summary>
/// Function invocation middleware that intercepts <c>SearchSuggestionAsync</c> tool calls
/// and executes them directly via <see cref="AIAgentBuilder"/>.
/// </summary>
/// <example>
/// Plug into an <see cref="AIAgentBuilder"/> pipeline:
/// <code>
/// AIAgent agent = new AIAgentBuilder(originalAgent)
///     .UseAzureMapsSearch(new MapsSearchConfig { AzureMapsKey = "&lt;key&gt;" })
///     .Build();
/// </code>
/// </example>
public static class MapsSearchTools
{
    private static MapsSearchConfig _config = new();

    /// <summary>
    /// Creates the Azure Maps suggestion <see cref="AITool"/> array to pass into <see cref="ChatClientAgent"/>.
    /// </summary>
    /// <param name="config">Optional search configuration. Uses defaults when <see langword="null"/>.</param>
    public static AITool[] Create(MapsSearchConfig? config = null)
    {
        _config = config ?? new MapsSearchConfig();
        return MapsSearchToolFactory.CreateTools(_config).Cast<AITool>().ToArray();
    }

    /// <summary>
    /// Extension method — registers Azure Maps search middleware on an <see cref="AIAgentBuilder"/>.
    /// Enables the <c>.UseAzureMapsSearch(config)</c> syntax.
    /// </summary>
    public static AIAgentBuilder UseAzureMapsSearch(this AIAgentBuilder builder, MapsSearchConfig? config = null)
    {
        _config = config ?? new MapsSearchConfig();
        MapsSearchHandler.Configure(_config);
        return builder.Use(Invoke);
    }

    /// <summary>
    /// Middleware delegate compatible with <see cref="AIAgentBuilder.Use"/>.
    /// Intercepts <c>SearchSuggestionAsync</c> tool calls; all other calls are passed to
    /// <paramref name="next"/>.
    /// </summary>
    private static async ValueTask<object?> Invoke(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        return context.Function.Name switch
        {
            MapsSearchToolNames.SearchSuggestion => await HandleSearchSuggestion(context.Arguments, cancellationToken),
            _ => await next(context, cancellationToken)
        };
    }

    private static async Task<object?> HandleSearchSuggestion(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var suggestedLocationTypes = GetString(args, "suggestedLocationTypes", string.Empty);
        var location               = GetString(args, "location", string.Empty);

        var addresses = await MapsSearchHandler.SearchSuggestionAsync(
            suggestedLocationTypes,
            location,
            cancellationToken);

        if (addresses is not { Count: > 0 })
            return "No results found.";

        var activeFields = _config.ActiveFields;
        var result = string.Join(Environment.NewLine, addresses.Select(a => a.ToString(activeFields)));

        Console.WriteLine(result);

        return result;
    }

    private static string GetString(
        IReadOnlyDictionary<string, object?> args,
        string key,
        string defaultValue)
    {
        if (!args.TryGetValue(key, out var value))
            return defaultValue;

        var raw = value?.ToString() ?? defaultValue;
        var trimmed = raw.Trim();

        try
        {
            // JSON string: "coffee shop" → coffee shop
            if (trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            {
                var unwrapped = System.Text.Json.JsonSerializer.Deserialize<string>(trimmed);
                if (unwrapped is not null)
                    return unwrapped;
            }

            // JSON array: ["coffee shop"] → coffee shop (first element)
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var array = System.Text.Json.JsonSerializer.Deserialize<string[]>(trimmed);
                if (array is { Length: > 0 } && array[0] is not null)
                    return array[0];
            }
        }
        catch (System.Text.Json.JsonException) { /* not valid JSON — return raw */ }

        return raw;
    }
}
