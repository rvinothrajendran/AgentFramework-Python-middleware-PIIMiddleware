namespace AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;

/// <summary>
/// Defines a storage contract for per-user, per-period token quota tracking.
/// </summary>
public interface IQuotaStore
{
    /// <summary>Returns the accumulated token count for <paramref name="userId"/> in the given <paramref name="periodKey"/>.</summary>
    long GetUsage(string userId, string periodKey);

    /// <summary>Adds <paramref name="tokens"/> to the running total for <paramref name="userId"/> in the given <paramref name="periodKey"/>.</summary>
    void AddUsage(string userId, string periodKey, long tokens);
}
