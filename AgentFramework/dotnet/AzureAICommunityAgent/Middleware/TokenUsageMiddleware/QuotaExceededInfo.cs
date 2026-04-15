namespace AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;

/// <summary>
/// Context passed to the <c>onQuotaExceeded</c> callback when a user's token
/// quota has been exhausted, before the <see cref="QuotaExceededException"/> is thrown.
/// </summary>
public sealed class QuotaExceededInfo
{
    /// <summary>The user identifier whose quota was exceeded.</summary>
    public string UserId { get; }

    /// <summary>The period key (e.g. "2026-04") during which the quota was exceeded.</summary>
    public string PeriodKey { get; }

    /// <summary>Tokens already consumed in the current period at the time of the check.</summary>
    public long UsedTokens { get; }

    /// <summary>The maximum tokens allowed for the current period.</summary>
    public long QuotaTokens { get; }

    public QuotaExceededInfo(string userId, string periodKey, long usedTokens, long quotaTokens)
    {
        UserId = userId;
        PeriodKey = periodKey;
        UsedTokens = usedTokens;
        QuotaTokens = quotaTokens;
    }
}
