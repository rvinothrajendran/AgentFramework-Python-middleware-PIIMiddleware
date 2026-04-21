namespace AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware;

/// <summary>
/// Thrown when the token threshold is reached and the <c>onThresholdReached</c>
/// callback returns <see langword="false"/>, indicating the request should be blocked
/// rather than compressed.
/// </summary>
public sealed class ContextCompressionThresholdException : Exception
{
    /// <summary>Estimated token count that triggered the threshold.</summary>
    public int TokensUsed { get; }

    /// <summary>The configured maximum token limit.</summary>
    public int MaxTokens { get; }

    public ContextCompressionThresholdException(int tokensUsed, int maxTokens)
        : base($"Token threshold reached: {tokensUsed} / {maxTokens}. Request blocked by onThresholdReached callback.")
    {
        TokensUsed = tokensUsed;
        MaxTokens = maxTokens;
    }
}
