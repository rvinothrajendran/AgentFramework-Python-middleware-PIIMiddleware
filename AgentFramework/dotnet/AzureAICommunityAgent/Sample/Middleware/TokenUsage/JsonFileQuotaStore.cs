using System.Text.Json;
using AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;

namespace TokenUsage;

/// <summary>
/// A file-backed <see cref="IQuotaStore"/> that persists per-user token usage
/// to a JSON file on every write, surviving process restarts.
/// </summary>
/// <remarks>
/// The file is created automatically if it does not exist.
/// All reads and writes are protected by a <see cref="Lock"/> so the store is
/// safe for concurrent access within a single process.
/// For multi-process deployments supply a shared backend instead.
/// </remarks>
public sealed class JsonFileQuotaStore : IQuotaStore
{
    // { "userId|periodKey": tokenCount }
    private readonly Dictionary<string, long> _totals;
    private readonly string _filePath;
    private readonly Lock   _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Initialises the store, loading any existing data from <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">
    ///   Path to the JSON file. The directory is created automatically if absent.
    /// </param>
    public JsonFileQuotaStore(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _totals   = Load(filePath);
    }

    /// <inheritdoc/>
    public long GetUsage(string userId, string periodKey)
    {
        lock (_lock)
            return _totals.GetValueOrDefault(Key(userId, periodKey), 0L);
    }

    /// <inheritdoc/>
    public void AddUsage(string userId, string periodKey, long tokens)
    {
        lock (_lock)
        {
            var key = Key(userId, periodKey);
            _totals[key] = _totals.TryGetValue(key, out var current) ? current + tokens : tokens;
            Persist();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────── //

    private void Persist()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(_filePath, JsonSerializer.Serialize(_totals, JsonOptions));
    }

    private static Dictionary<string, long> Load(string filePath)
    {
        if (!File.Exists(filePath))
            return [];

        try
        {
            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
                return [];

            return JsonSerializer.Deserialize<Dictionary<string, long>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static string Key(string userId, string periodKey)
    {
        // Escape any backslashes first, then escape pipes, so the composite key
        // is unambiguous even when userId or periodKey contains '|' characters.
        var escapedUserId    = userId.Replace(@"\", @"\\").Replace("|", @"\|");
        var escapedPeriodKey = periodKey.Replace(@"\", @"\\").Replace("|", @"\|");
        return $"{escapedUserId}|{escapedPeriodKey}";
    }
}
