namespace AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;

/// <summary>
/// An immutable snapshot of token usage emitted to the <c>onUsage</c> callback
/// after every successful completion.
/// </summary>
public sealed class TokenUsageRecord
{
    /// <summary>The user identifier the tokens were charged to.</summary>
    public string UserId { get; }

    /// <summary>The period key (e.g. "2026-04") during which the call was made.</summary>
    public string PeriodKey { get; }

    /// <summary>The model identifier returned by the provider, if available.</summary>
    public string? Model { get; }

    /// <summary>Prompt / input tokens reported by the provider, if available.</summary>
    public long? InputTokens { get; }

    /// <summary>Completion / output tokens reported by the provider, if available.</summary>
    public long? OutputTokens { get; }

    /// <summary>Total tokens consumed by this call (input + output, or the explicit total if provided).</summary>
    public long TotalTokens { get; }

    /// <summary>The configured quota limit for the current period.</summary>
    public long QuotaTokens { get; }

    /// <summary>Accumulated token count for the user after this call has been recorded.</summary>
    public long UsedTokensAfterCall { get; }

    /// <summary><see langword="true"/> if the record was captured from a streaming response.</summary>
    public bool IsStreaming { get; }

    /// <summary>UTC timestamp at the moment this record was created.</summary>
    public DateTimeOffset TimestampUtc { get; }

    public TokenUsageRecord(
        string userId,
        string periodKey,
        string? model,
        long? inputTokens,
        long? outputTokens,
        long totalTokens,
        long quotaTokens,
        long usedTokensAfterCall,
        bool isStreaming)
    {
        UserId = userId;
        PeriodKey = periodKey;
        Model = model;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        TotalTokens = totalTokens;
        QuotaTokens = quotaTokens;
        UsedTokensAfterCall = usedTokensAfterCall;
        IsStreaming = isStreaming;
        TimestampUtc = DateTimeOffset.UtcNow;
    }
}
