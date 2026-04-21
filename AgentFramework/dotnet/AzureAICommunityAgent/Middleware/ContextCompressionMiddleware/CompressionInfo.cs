namespace AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware;

/// <summary>
/// Information passed to the <c>onThresholdReached</c> callback when the estimated
/// token count meets or exceeds the configured trigger threshold.
/// </summary>
/// <param name="TokensUsed">Estimated token count of the current message history.</param>
/// <param name="MaxTokens">The configured maximum token limit.</param>
/// <param name="TriggerTokens">The threshold at which compression is triggered.</param>
public sealed record CompressionInfo(int TokensUsed, int MaxTokens, int TriggerTokens);
