using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;

/// <summary>
/// A delegating chat client middleware that enforces per-user token quotas and
/// emits a <see cref="TokenUsageRecord"/> after every successful completion.
/// </summary>
/// <remarks>
/// <para>
/// Before each request the middleware checks the user's accumulated token count
/// for the current period against <see cref="QuotaTokens"/>. If the quota is
/// already exhausted an optional <c>onQuotaExceeded</c> callback fires and a
/// <see cref="QuotaExceededException"/> is thrown — the inner client is never invoked.
/// </para>
/// <para>
/// After a successful completion (non-streaming or streaming) the token usage
/// reported by the model is persisted via <see cref="IQuotaStore"/> and the
/// optional <c>onUsage</c> callback is invoked with a <see cref="TokenUsageRecord"/>.
/// </para>
/// <para>
/// The user identifier defaults to the <c>"user_id"</c> entry in
/// <see cref="ChatOptions.AdditionalProperties"/>, falling back to <c>"anonymous"</c>.
/// Provide a custom <c>userIdGetter</c> delegate to override this behaviour.
/// </para>
/// </remarks>
public sealed class TokenUsageMiddleware : DelegatingChatClient
{
    private readonly IQuotaStore _quotaStore;
    private readonly long _quotaTokens;
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, string> _userIdGetter;
    private readonly Func<string> _periodKeyFn;
    private readonly Func<TokenUsageRecord, CancellationToken, Task>? _onUsage;
    private readonly Func<QuotaExceededInfo, CancellationToken, Task>? _onQuotaExceeded;

    /// <summary>The configured maximum tokens per user per period.</summary>
    public long QuotaTokens => _quotaTokens;

    /// <summary>
    /// Creates a new <see cref="TokenUsageMiddleware"/>.
    /// </summary>
    /// <param name="inner">The inner <see cref="IChatClient"/> to delegate to.</param>
    /// <param name="quotaStore">
    ///   Persistent store that tracks per-user token consumption.
    ///   Use <see cref="InMemoryQuotaStore"/> for in-process scenarios or supply a
    ///   shared backend for multi-process deployments.
    /// </param>
    /// <param name="quotaTokens">Maximum tokens a single user may consume in one period. Must be &gt; 0.</param>
    /// <param name="onUsage">
    ///   Optional async callback invoked after every successful completion with a
    ///   <see cref="TokenUsageRecord"/>. Use it to persist metrics or forward to a billing system.
    /// </param>
    /// <param name="onQuotaExceeded">
    ///   Optional async callback invoked when a pre-call quota check fails, before the
    ///   <see cref="QuotaExceededException"/> is thrown.
    /// </param>
    /// <param name="userIdGetter">
    ///   Extracts the current user's identifier from the request. Defaults to reading
    ///   <c>"user_id"</c> from <see cref="ChatOptions.AdditionalProperties"/>, falling
    ///   back to <c>"anonymous"</c>.
    /// </param>
    /// <param name="periodKeyFn">
    ///   Returns the current quota period identifier. Defaults to <see cref="PeriodKeys.Month"/>.
    ///   Use <see cref="PeriodKeys.Day"/> or <see cref="PeriodKeys.Week"/> for finer granularity,
    ///   or supply any custom delegate.
    /// </param>
    public TokenUsageMiddleware(
        IChatClient inner,
        IQuotaStore quotaStore,
        long quotaTokens,
        Func<TokenUsageRecord, CancellationToken, Task>? onUsage = null,
        Func<QuotaExceededInfo, CancellationToken, Task>? onQuotaExceeded = null,
        Func<IEnumerable<ChatMessage>, ChatOptions?, string>? userIdGetter = null,
        Func<string>? periodKeyFn = null)
        : base(inner)
    {
        _quotaStore = quotaStore ?? throw new ArgumentNullException(nameof(quotaStore));
        _quotaTokens = quotaTokens > 0
            ? quotaTokens
            : throw new ArgumentOutOfRangeException(nameof(quotaTokens), "Quota must be greater than zero.");
        _onUsage = onUsage;
        _onQuotaExceeded = onQuotaExceeded;
        _userIdGetter = userIdGetter ?? DefaultUserIdGetter;
        _periodKeyFn = periodKeyFn ?? PeriodKeys.Month;
    }

    // ── Non-streaming ─────────────────────────────────────────────────────── //

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _userIdGetter(messages, options);
        var periodKey = _periodKeyFn();

        await EnforceQuotaAsync(userId, periodKey, cancellationToken).ConfigureAwait(false);

        var response = await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);

        await RecordUsageAsync(
            userId, periodKey,
            model: response.ModelId,
            inputTokens: response.Usage?.InputTokenCount,
            outputTokens: response.Usage?.OutputTokenCount,
            totalTokens: response.Usage?.TotalTokenCount,
            isStreaming: false,
            cancellationToken).ConfigureAwait(false);

        return response;
    }

    // ── Streaming ─────────────────────────────────────────────────────────── //

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userId = _userIdGetter(messages, options);
        var periodKey = _periodKeyFn();

        await EnforceQuotaAsync(userId, periodKey, cancellationToken).ConfigureAwait(false);

        (string? model, long? input, long? output, long? total) usage = default;

        await foreach (var update in base
            .GetStreamingResponseAsync(messages, options, cancellationToken)
            .ConfigureAwait(false))
        {
            usage = MergeStreamingUsage(usage, update);
            yield return update;
        }

        await RecordUsageAsync(
            userId, periodKey,
            model:        usage.model,
            inputTokens:  usage.input,
            outputTokens: usage.output,
            totalTokens:  usage.total,
            isStreaming:  true,
            cancellationToken).ConfigureAwait(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────── //

    private async Task EnforceQuotaAsync(string userId, string periodKey, CancellationToken ct)
    {
        var used = _quotaStore.GetUsage(userId, periodKey);
        if (used < _quotaTokens)
            return;

        var info = new QuotaExceededInfo(userId, periodKey, used, _quotaTokens);
        if (_onQuotaExceeded is not null)
            await _onQuotaExceeded(info, ct).ConfigureAwait(false);

        throw new QuotaExceededException(userId, periodKey, used, _quotaTokens);
    }

    private async Task RecordUsageAsync(
        string userId,
        string periodKey,
        string? model,
        long? inputTokens,
        long? outputTokens,
        long? totalTokens,
        bool isStreaming,
        CancellationToken ct)
    {
        long total = totalTokens ?? (inputTokens ?? 0L) + (outputTokens ?? 0L);
        if (total <= 0)
            return;

        _quotaStore.AddUsage(userId, periodKey, total);

        if (_onUsage is null)
            return;

        var record = new TokenUsageRecord(
            userId: userId,
            periodKey: periodKey,
            model: model,
            inputTokens: inputTokens,
            outputTokens: outputTokens,
            totalTokens: total,
            quotaTokens: _quotaTokens,
            usedTokensAfterCall: _quotaStore.GetUsage(userId, periodKey),
            isStreaming: isStreaming);

        await _onUsage(record, ct).ConfigureAwait(false);
    }

    private static (string? model, long? input, long? output, long? total) MergeStreamingUsage(
        (string? model, long? input, long? output, long? total) acc,
        ChatResponseUpdate update)
    {
        var model  = acc.model ?? update.ModelId;
        var input  = acc.input;
        var output = acc.output;
        var total  = acc.total;

        foreach (var item in update.Contents)
        {
            if (item is UsageContent uc)
            {
                input  = uc.Details.InputTokenCount  ?? input;
                output = uc.Details.OutputTokenCount ?? output;
                total  = uc.Details.TotalTokenCount  ?? total;
            }
        }

        return (model, input, output, total);
    }

    private static string DefaultUserIdGetter(IEnumerable<ChatMessage> _, ChatOptions? options)
    {
        if (options?.AdditionalProperties?.TryGetValue("user_id", out var raw) == true
            && raw is string id
            && !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return "anonymous";
    }
}
