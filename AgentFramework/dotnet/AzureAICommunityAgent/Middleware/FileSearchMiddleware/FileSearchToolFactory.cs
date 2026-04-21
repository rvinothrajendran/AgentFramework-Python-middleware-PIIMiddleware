using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.FileSearchMiddleware;

/// <summary>
/// Creates <see cref="AIFunction"/> tool instances for the file-search tools,
/// ready to be registered with a Microsoft.Extensions.AI chat pipeline.
/// </summary>
/// <example>
/// Register with a chat client:
/// <code>
/// var config = new SearchConfig { MaxResults = 50, SkipHidden = true };
/// var tools  = FileSearchToolFactory.Create(config).ToList();
///
/// var options = new ChatOptions { Tools = tools };
/// var response = await client.GetResponseAsync(messages, options);
/// </code>
/// </example>
public static class FileSearchToolFactory
{
    /// <summary>
    /// Returns the two file-search <see cref="AIFunction"/> tools:
    /// <c>SearchByName</c> and <c>SearchByContent</c>.
    /// </summary>
    /// <param name="config">
    ///   Optional configuration applied to every search call made through the returned tools.
    ///   When <see langword="null"/>, <see cref="SearchConfig"/> defaults are used.
    /// </param>
    public static IEnumerable<AIFunction> CreateTools(SearchConfig? config = null)
    {
        var cfg = config ?? new SearchConfig();

        yield return AIFunctionFactory.Create(
            (
                [Description("Glob pattern to match against file names (e.g. '*.cs', 'Program*'). Bare extensions are auto-normalised: 'cs' becomes '*.cs'. Use '*' to match all files.")] string query,
                [Description("Root directory to search in. Defaults to the current directory.")] string path,
                [Description("Whether the glob match is case-sensitive. Defaults to false.")] bool caseSensitive,
                [Description("Optional list of extensions to match (e.g. ['cs', 'txt']). A file must satisfy both the glob and the type filter. Pass null to match any extension.")] string[]? fileTypes
            ) => FileSearchHandler.SearchByName(query, path, caseSensitive, fileTypes, cfg),
            new AIFunctionFactoryOptions
            {
                Name = FileSearchToolNames.SearchByName,
                Description = "Search for files whose names match a glob pattern under a directory. " +
                              "Returns a list of matching file paths relative to the search root, " +
                              "capped at the configured maximum."
            });

        yield return AIFunctionFactory.Create(
            (
                [Description("Plain-text string to search for inside files.")] string query,
                [Description("Root directory to search in. Defaults to the current directory.")] string path,
                [Description("Whether the string match is case-sensitive. Defaults to true.")] bool caseSensitive,
                [Description("Optional list of extensions to restrict the search to (e.g. ['cs', 'txt']). Pass null to search all file types.")] string[]? fileTypes
            ) => FileSearchHandler.SearchByContent(query, path, caseSensitive, fileTypes, cfg),
            new AIFunctionFactoryOptions
            {
                Name = FileSearchToolNames.SearchByContent,
                Description = "Search for files whose contents contain a given plain-text string. " +
                              "Skips binary files and files exceeding the configured size limit. " +
                              "Returns a list of file paths relative to the search root that contain the query string."
            });
    }
}
