using System.IO.Enumeration;
using System.Text;

namespace AzureAICommunity.Agent.Middleware.FileSearchMiddleware;

/// <summary>
/// Core file-search logic used by <see cref="FileSearchToolFactory"/>.
/// Can also be called directly without an AI pipeline.
/// </summary>
public static class FileSearchHandler
{
    private static readonly SearchConfig DefaultConfig = new();

    /// <summary>
    /// Search for files whose names match a glob pattern under a directory.
    /// </summary>
    /// <param name="query">Glob pattern matched against file names. Bare extensions are auto-normalised. Defaults to <c>"*"</c> (all files).</param>
    /// <param name="path">Root directory to search. Defaults to <c>"."</c>.</param>
    /// <param name="caseSensitive">Whether the glob match is case-sensitive. Default: <see langword="false"/>.</param>
    /// <param name="fileTypes">Optional list of extensions to match. A file must satisfy both the glob and the type filter.</param>
    /// <param name="config">Per-call configuration override. Uses module default when <see langword="null"/>.</param>
    /// <returns>Matching file paths relative to <paramref name="path"/>, capped at <see cref="SearchConfig.MaxResults"/>.</returns>
    /// <exception cref="ArgumentException">Query or path are blank.</exception>
    /// <exception cref="DirectoryNotFoundException">Path does not exist.</exception>
    public static IReadOnlyList<string> SearchByName(
        string query,
        string path = ".",
        bool caseSensitive = false,
        IReadOnlyList<string>? fileTypes = null,
        SearchConfig? config = null)
    {
        var cfg = config ?? DefaultConfig;
        var root = ValidateAndResolvePath(query, path);

        var q = query.Trim();
        if (q.Length > 0 && !q.Contains('*') && !q.Contains('?') && !q.Contains('['))
        {
            if (q.StartsWith('.'))
                q = $"*{q}";
            else if (!q.Contains('.'))
                q = $"*.{q}";
        }

        var allowedTypes = NormalizeExtensions(fileTypes);
        var matches = new List<string>();
        var seenDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        WalkDirectory(root, root, 0, cfg, seenDirs, matches, (file, filename) =>
        {
            if (allowedTypes is not null &&
                !allowedTypes.Contains(Path.GetExtension(filename).ToLowerInvariant()))
                return;

            if (FileSystemName.MatchesSimpleExpression(q, filename, !caseSensitive))
                matches.Add(Path.GetRelativePath(root, file));
        });

        return matches;
    }

    /// <summary>
    /// Search for files whose contents contain a given string.
    /// </summary>
    /// <param name="query">Plain-text string to search for inside files.</param>
    /// <param name="path">Root directory to search. Defaults to <c>"."</c>.</param>
    /// <param name="caseSensitive">Whether the string match is case-sensitive. Default: <see langword="true"/>.</param>
    /// <param name="fileTypes">Optional list of extensions to restrict the search to.</param>
    /// <param name="config">Per-call configuration override. Uses module default when <see langword="null"/>.</param>
    /// <returns>
    ///   File paths (relative to <paramref name="path"/>) that contain <paramref name="query"/>,
    ///   capped at <see cref="SearchConfig.MaxResults"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Query or path are blank.</exception>
    /// <exception cref="DirectoryNotFoundException">Path does not exist.</exception>
    public static IReadOnlyList<string> SearchByContent(
        string query,
        string path = ".",
        bool caseSensitive = true,
        IReadOnlyList<string>? fileTypes = null,
        SearchConfig? config = null)
    {
        var cfg = config ?? DefaultConfig;
        var root = ValidateAndResolvePath(query, path);
        var needle = query.Trim();
        if (!caseSensitive) needle = needle.ToLowerInvariant();

        var allowedTypes = NormalizeExtensions(fileTypes);
        var matches = new List<string>();
        var seenDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        WalkDirectory(root, root, 0, cfg, seenDirs, matches, (file, filename) =>
        {
            if (allowedTypes is not null &&
                !allowedTypes.Contains(Path.GetExtension(filename).ToLowerInvariant()))
                return;

            try
            {
                if (new FileInfo(file).Length > cfg.MaxFileSizeBytes) return;
            }
            catch (IOException) { return; }

            if (IsBinary(file, cfg.BinaryCheckBytes)) return;

            var content = TryReadFile(file, cfg.Encodings);
            if (content is null) return;

            var haystack = caseSensitive ? content : content.ToLowerInvariant();
            if (haystack.Contains(needle))
                matches.Add(Path.GetRelativePath(root, file));
        });

        return matches;
    }

    // ── Helpers ───────────────────────────────────────────────────────────── //

    private static string ValidateAndResolvePath(string query, string path)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("query must be a non-empty string.", nameof(query));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must be a non-empty string.", nameof(path));

        var root = Path.GetFullPath(path);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"Search path does not exist: {path}");
        return root;
    }

    internal static HashSet<string>? NormalizeExtensions(IReadOnlyList<string>? types)
    {
        if (types is null || types.Count == 0) return null;
        return new HashSet<string>(
            types.Select(t => t.StartsWith('.') ? t.ToLowerInvariant() : $".{t.ToLowerInvariant()}"),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsExtensionAllowed(string filename, SearchConfig cfg)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();

        if (cfg.ExcludeExtensions?.Any(e =>
                string.Equals(e.StartsWith('.') ? e : $".{e}", ext,
                    StringComparison.OrdinalIgnoreCase)) == true)
            return false;

        if (cfg.IncludeExtensions is not null &&
            !cfg.IncludeExtensions.Any(e =>
                string.Equals(e.StartsWith('.') ? e : $".{e}", ext,
                    StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }

    private static bool IsBinary(string filePath, int checkBytes)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[checkBytes];
            var read = fs.Read(buffer, 0, buffer.Length);
            return Array.IndexOf(buffer, (byte)0, 0, read) >= 0;
        }
        catch { return true; }
    }

    private static string? TryReadFile(string filePath, IReadOnlyList<string> encodings)
    {
        foreach (var enc in encodings)
        {
            try
            {
                return File.ReadAllText(filePath, Encoding.GetEncoding(enc));
            }
            catch { /* try next encoding */ }
        }
        return null;
    }

    /// <summary>
    /// Recursively walks <paramref name="dir"/>, calling <paramref name="processFile"/>
    /// for each eligible file, stopping once <see cref="SearchConfig.MaxResults"/> is reached.
    /// </summary>
    private static void WalkDirectory(
        string dir,
        string root,
        int depth,
        SearchConfig cfg,
        HashSet<string> seenDirs,
        List<string> matches,
        Action<string, string> processFile)
    {
        if (matches.Count >= cfg.MaxResults) return;

        // Symlink-loop guard
        var realDir = Path.GetFullPath(dir);
        if (!seenDirs.Add(realDir)) return;

        if (depth >= cfg.MaxDepth) return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (matches.Count >= cfg.MaxResults) return;

                var filename = Path.GetFileName(file);
                if (cfg.SkipHidden && filename.StartsWith('.')) continue;
                if (!IsExtensionAllowed(filename, cfg)) continue;

                processFile(file, filename);
            }

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                if (matches.Count >= cfg.MaxResults) return;

                var dirname = Path.GetFileName(subDir);
                if (cfg.SkipHidden && dirname.StartsWith('.')) continue;

                // Skip symlinked directories unless FollowSymlinks is enabled
                if (!cfg.FollowSymlinks &&
                    new DirectoryInfo(subDir).Attributes.HasFlag(FileAttributes.ReparsePoint))
                    continue;

                WalkDirectory(subDir, root, depth + 1, cfg, seenDirs, matches, processFile);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }
        catch (IOException) { }
    }
}
