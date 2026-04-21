namespace AzureAICommunity.Agent.Middleware.FileSearchMiddleware;

/// <summary>
/// Runtime configuration for the file-search tools.
/// All fields have sensible defaults; override only what you need.
/// </summary>
/// <example>
/// Per-call override:
/// <code>
/// var results = FileSearchHandler.SearchByName("*.cs", config: new SearchConfig { MaxResults = 50 });
/// </code>
/// Shared config passed to the tool factory:
/// <code>
/// var tools = FileSearchToolFactory.Create(new SearchConfig
/// {
///     MaxResults   = 100,
///     MaxDepth     = 5,
///     SkipHidden   = true,
///     ExcludeExtensions = [".log", ".tmp"],
/// });
/// </code>
/// </example>
public sealed class SearchConfig
{
    /// <summary>Maximum number of paths returned by any single search call. Default: 200.</summary>
    public int MaxResults { get; init; } = 200;

    /// <summary>Files larger than this are skipped during content search. Default: 10 MB.</summary>
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    /// <summary>Maximum directory depth to recurse into (prevents runaway traversal). Default: 20.</summary>
    public int MaxDepth { get; init; } = 20;

    /// <summary>Number of bytes read to detect binary files (null-byte probe). Default: 8192.</summary>
    public int BinaryCheckBytes { get; init; } = 8192;

    /// <summary>Whether to follow symbolic links while walking the directory tree. Default: false.</summary>
    public bool FollowSymlinks { get; init; } = false;

    /// <summary>Skip files and directories whose names start with a dot. Default: false.</summary>
    public bool SkipHidden { get; init; } = false;

    /// <summary>
    /// Whitelist of file extensions (e.g. <c>".cs"</c>, <c>".txt"</c>).
    /// When set, only files with a matching extension are returned.
    /// <see langword="null"/> means all extensions are allowed.
    /// </summary>
    public IReadOnlyList<string>? IncludeExtensions { get; init; }

    /// <summary>
    /// Blacklist of file extensions (e.g. <c>".log"</c>, <c>".tmp"</c>).
    /// Files with a matching extension are always skipped.
    /// </summary>
    public IReadOnlyList<string>? ExcludeExtensions { get; init; }

    /// <summary>
    /// Encoding fallback chain used when reading files for content search.
    /// Default: <c>["utf-8", "latin-1"]</c>.
    /// </summary>
    public IReadOnlyList<string> Encodings { get; init; } = ["utf-8", "latin-1"];

    /// <summary>
    /// Fallback root directory used when the LLM does not supply a path argument.
    /// Defaults to the current working directory (<c>"."</c>).
    /// Set this to a specific folder to ensure searches always target the right location
    /// even when the user prompt does not mention a path.
    /// </summary>
    public string DefaultPath { get; init; } = ".";
}
