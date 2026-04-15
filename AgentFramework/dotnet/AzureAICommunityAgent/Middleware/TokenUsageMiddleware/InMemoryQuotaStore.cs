namespace AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;

/// <summary>
/// Thread-unsafe, in-process quota store backed by a plain dictionary.
/// </summary>
/// <remarks>
/// Safe for use within a single async execution flow where no concurrent operations
/// interleave between <see cref="GetUsage"/> and <see cref="AddUsage"/>.
/// For multi-process or multi-threaded scenarios supply a shared backend
/// (Redis, SQL, etc.) through <see cref="IQuotaStore"/>.
/// </remarks>
public sealed class InMemoryQuotaStore : IQuotaStore
{
    private readonly Dictionary<(string UserId, string PeriodKey), long> _totals = new();

    /// <inheritdoc/>
    public long GetUsage(string userId, string periodKey) =>
        _totals.GetValueOrDefault((userId, periodKey), 0L);

    /// <inheritdoc/>
    public void AddUsage(string userId, string periodKey, long tokens)
    {
        var key = (userId, periodKey);
        _totals[key] = _totals.TryGetValue(key, out var current) ? current + tokens : tokens;
    }
}
