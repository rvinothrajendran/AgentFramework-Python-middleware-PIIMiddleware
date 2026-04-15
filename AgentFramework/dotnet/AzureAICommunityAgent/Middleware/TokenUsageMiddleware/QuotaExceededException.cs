namespace AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;

/// <summary>
/// Thrown when a user's token quota for the current period has been exhausted.
/// </summary>
public sealed class QuotaExceededException : Exception
{
    /// <summary>The user identifier whose quota was exceeded.</summary>
    public string UserId { get; }

    /// <summary>The period key (e.g. "2026-04") during which the quota was exceeded.</summary>
    public string PeriodKey { get; }

    /// <summary>Tokens already consumed in the current period at the time of the check.</summary>
    public long UsedTokens { get; }

    /// <summary>The maximum tokens allowed for the current period.</summary>
    public long QuotaTokens { get; }

    public QuotaExceededException(string userId, string periodKey, long usedTokens, long quotaTokens)
        : base($"Token quota exceeded for user='{userId}', period='{periodKey}'. Used={usedTokens}, Quota={quotaTokens}.")
    {
        UserId = userId;
        PeriodKey = periodKey;
        UsedTokens = usedTokens;
        QuotaTokens = quotaTokens;
    }
}
