using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.FileSearchMiddleware;

/// <summary>
/// Function invocation middleware that intercepts <c>SearchByName</c> and
/// <c>SearchByContent</c> tool calls and executes them directly via
/// <see cref="AIAgentBuilder"/>.
/// </summary>
/// <example>
/// Plug into an <see cref="FileSearchHandler"/> pipeline
/// <code>
/// AIAgent agent = new AIAgentBuilder(originalAgent)
///     .UseFileSearch()
///     .Build();
/// </code>
/// </example>
public static class FileSearchTools
{
    private static SearchConfig _config = new();

    /// <summary>
    /// Creates the file-search <see cref="AITool"/> array to pass into <see cref="ChatClientAgent"/>.
    /// </summary>
    /// <param name="config">Optional search configuration. Uses defaults when <see langword="null"/>.</param>
    public static AITool[] Create(SearchConfig? config = null)
    {
        _config = config ?? new SearchConfig();
        return FileSearchToolFactory.CreateTools(_config).Cast<AITool>().ToArray();
    }

    /// <summary>
    /// Extension method — registers file-search middleware on an <see cref="AIAgentBuilder"/>.
    /// Enables the <c>.UseFileSearch()</c> syntax.
    /// </summary>
    public static AIAgentBuilder UseFileSearch(this AIAgentBuilder builder)
        => builder.Use(Invoke);

    /// <summary>
    /// Middleware delegate compatible with <see cref="AIAgentBuilder.Use"/>.
    /// Intercepts file-search tool calls; all other calls are passed to <paramref name="next"/>.
    /// When the LLM does not supply a path, falls back to <see cref="SearchConfig.DefaultPath"/>.
    /// </summary>
    private static async ValueTask<object?> Invoke(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        return context.Function.Name switch
        {
            FileSearchToolNames.SearchByName => HandleSearchByName(context.Arguments),
            FileSearchToolNames.SearchByContent => HandleSearchByContent(context.Arguments),
            _ =>  await next(context, cancellationToken)
        };
    }

    private static string GetPath(IReadOnlyDictionary<string, object?> args)
    {
        var value = GetString(args, "path", defaultValue: string.Empty);
        return string.IsNullOrWhiteSpace(value) ? _config.DefaultPath : value;
    }


    private static object HandleSearchByName(
        IReadOnlyDictionary<string, object?> args)
    {
        var query = GetString(args, "query", "*");
        var path = GetString(args, "path", ".");
        var caseSensitive = GetBool(args, "caseSensitive", false);
        var fileTypes = GetList(args, "fileTypes");

        return FileSearchHandler.SearchByName(
            query: query,
            path: path,
            caseSensitive: caseSensitive,
            fileTypes: fileTypes);
    }

    private static object HandleSearchByContent(
        IReadOnlyDictionary<string, object?> args)
    {
        var query = GetString(args, "query", string.Empty);
        var path = GetString(args, "path", ".");
        var caseSensitive = GetBool(args, "caseSensitive", true);
        var fileTypes = GetList(args, "fileTypes");

        return FileSearchHandler.SearchByContent(
            query: query,
            path: path,
            caseSensitive: caseSensitive,
            fileTypes: fileTypes);
    }

    private static string GetString(
        IReadOnlyDictionary<string, object?> args,
        string key,
        string defaultValue)
    {
        if (!args.TryGetValue(key, out var value))
            return defaultValue;

        return value?.ToString() ?? defaultValue;
    }

    private static bool GetBool(
        IReadOnlyDictionary<string, object?> args,
        string key,
        bool defaultValue)
    {
        if (!args.TryGetValue(key, out var value))
            return defaultValue;

        return value is bool flag
            ? flag
            : defaultValue;
    }

    private static IReadOnlyList<string>? GetList(
        IReadOnlyDictionary<string, object?> args,
        string key)
    {
        if (!args.TryGetValue(key, out var value))
            return null;

        return value as IReadOnlyList<string>;
    }
}
