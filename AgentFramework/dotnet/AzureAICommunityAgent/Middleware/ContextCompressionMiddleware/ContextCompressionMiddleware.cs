using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Middleware.ContextCompressionMiddleware;

/// <summary>
/// A delegating chat client middleware that automatically compresses conversation
/// history by summarising older messages when the estimated token count approaches
/// the configured limit.
/// </summary>
/// <remarks>
/// <para>
/// Before each request the middleware estimates the total token count of all messages.
/// When it meets or exceeds the <see cref="TriggerTokens"/> threshold, older messages
/// are summarised via an LLM call and replaced with a single system summary message,
/// keeping only the most recent <c>keepLastMessages</c> messages intact.
/// </para>
/// <para>
/// An optional <c>onThresholdReached</c> callback fires when compression is triggered.
/// Returning <see langword="false"/> from the callback blocks the request and raises a
/// <see cref="ContextCompressionThresholdException"/> instead of compressing.
/// </para>
/// <para>
/// Token counting uses a character-based approximation (~4 characters per token) by
/// default. Supply a custom <c>tokenCounter</c> delegate to use a precise tokeniser
/// (e.g. via Microsoft.ML.Tokenizers).
/// </para>
/// <para>
/// Summarisation is performed using <c>summarizerClient</c> when provided, otherwise
/// it falls back to the <c>Inner</c> client, bypassing this middleware to avoid
/// infinite recursion.
/// </para>
/// </remarks>
public sealed class ContextCompressionMiddleware : DelegatingChatClient
{
    private readonly int _maxTokens;
    private readonly int _triggerTokens;
    private readonly int _keepLastMessages;
    private readonly Func<CompressionInfo, bool>? _onThresholdReached;
    private readonly Func<IEnumerable<ChatMessage>, int> _tokenCounter;
    private readonly IChatClient? _summarizerClient;

    /// <summary>The configured maximum token limit.</summary>
    public int MaxTokens => _maxTokens;

    /// <summary>The token count at which compression is triggered.</summary>
    public int TriggerTokens => _triggerTokens;

    /// <summary>
    /// Creates a new <see cref="ContextCompressionMiddleware"/>.
    /// </summary>
    /// <param name="inner">The inner <see cref="IChatClient"/> to delegate to.</param>
    /// <param name="maxTokens">
    ///   The maximum token budget for the conversation. Must be &gt; 0.
    /// </param>
    /// <param name="triggerRatio">
    ///   Fraction of <paramref name="maxTokens"/> at which compression triggers.
    ///   For example, <c>0.80</c> triggers at 80 % of the budget. Must be in (0, 1].
    /// </param>
    /// <param name="keepLastMessages">
    ///   Number of recent messages preserved verbatim after compression. Must be ≥ 0.
    /// </param>
    /// <param name="onThresholdReached">
    ///   Optional callback invoked when the threshold is hit. Return <see langword="true"/>
    ///   to allow compression (default behaviour) or <see langword="false"/> to block the
    ///   request and raise a <see cref="ContextCompressionThresholdException"/>.
    /// </param>
    /// <param name="tokenCounter">
    ///   Custom delegate that estimates the token count for a list of messages.
    ///   Defaults to <c>totalCharacters / 4</c> when <see langword="null"/>.
    /// </param>
    /// <param name="summarizerClient">
    ///   Optional separate <see cref="IChatClient"/> used exclusively for summarisation
    ///   calls. When <see langword="null"/> the middleware's own <c>Inner</c> client is
    ///   used, bypassing all wrapping middleware.
    /// </param>
    public ContextCompressionMiddleware(
        IChatClient inner,
        int maxTokens = 8000,
        double triggerRatio = 0.80,
        int keepLastMessages = 8,
        Func<CompressionInfo, bool>? onThresholdReached = null,
        Func<IEnumerable<ChatMessage>, int>? tokenCounter = null,
        IChatClient? summarizerClient = null)
        : base(inner)
    {
        if (maxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "Max tokens must be greater than zero.");
        if (triggerRatio is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(triggerRatio), "Trigger ratio must be between 0 (exclusive) and 1 (inclusive).");
        if (keepLastMessages < 0)
            throw new ArgumentOutOfRangeException(nameof(keepLastMessages), "Keep last messages must be zero or greater.");

        _maxTokens = maxTokens;
        _triggerTokens = (int)(maxTokens * triggerRatio);
        _keepLastMessages = keepLastMessages;
        _onThresholdReached = onThresholdReached;
        _tokenCounter = tokenCounter ?? DefaultTokenCounter;
        _summarizerClient = summarizerClient;
    }

    // ── Non-streaming ─────────────────────────────────────────────────────── //

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processed = await CompressIfNeededAsync(messages, cancellationToken).ConfigureAwait(false);
        return await base.GetResponseAsync(processed, options, cancellationToken).ConfigureAwait(false);
    }

    // ── Streaming ─────────────────────────────────────────────────────────── //

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var processed = await CompressIfNeededAsync(messages, cancellationToken).ConfigureAwait(false);

        await foreach (var update in base
            .GetStreamingResponseAsync(processed, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }
    }

    // ── Compression Logic ─────────────────────────────────────────────────── //

    private async Task<IList<ChatMessage>> CompressIfNeededAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return messageList;

        var tokenCount = _tokenCounter(messageList);
        if (tokenCount < _triggerTokens)
            return messageList;

        var info = new CompressionInfo(tokenCount, _maxTokens, _triggerTokens);

        if (_onThresholdReached is not null)
        {
            var allow = _onThresholdReached(info);
            if (!allow)
                throw new ContextCompressionThresholdException(tokenCount, _maxTokens);
        }

        var (oldMessages, recentMessages) = SplitMessages(messageList);
        if (oldMessages.Count == 0)
            return messageList; // nothing old to compress — avoid infinite loop

        var summary = await SummarizeAsync(oldMessages, ct).ConfigureAwait(false);

        var compressed = new List<ChatMessage>(recentMessages.Count + 1)
        {
            new ChatMessage(ChatRole.System, $"Conversation summary:\n{summary}")
        };
        compressed.AddRange(recentMessages);

        return compressed;
    }

    private (List<ChatMessage> Old, List<ChatMessage> Recent) SplitMessages(List<ChatMessage> messages)
    {
        var recent = new List<ChatMessage>();
        int i = messages.Count - 1;

        while (i >= 0 && recent.Count < _keepLastMessages)
        {
            recent.Insert(0, messages[i]);

            // keep assistant + tool pair together
            if (messages[i].Role == ChatRole.Tool && i > 0)
            {
                i--;
                recent.Insert(0, messages[i]);
            }

            i--;
        }

        var old = messages.Take(messages.Count - recent.Count).ToList();
        return (old, recent);
    }

    private async Task<string> SummarizeAsync(IList<ChatMessage> oldMessages, CancellationToken ct)
    {
        var sb = new StringBuilder();
        foreach (var m in oldMessages)
        {
            sb.AppendLine($"{m.Role}: {ExtractText(m)}");
        }

        var prompt = $"""
Summarize this conversation history.

Preserve:
- user goals
- facts
- decisions
- unresolved issues
- preferences
- tool outputs

Conversation:
{sb}
""";

        var client = _summarizerClient ?? InnerClient;
        var response = await client
            .GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], cancellationToken: ct)
            .ConfigureAwait(false);

        return response.Text ?? string.Empty;
    }

    // ── Default Token Counter ─────────────────────────────────────────────── //

    private static int DefaultTokenCounter(IEnumerable<ChatMessage> messages)
    {
        int charCount = 0;
        foreach (var m in messages)
        {
            charCount += m.Role.Value?.Length ?? 0;
            charCount += 2; // ": "
            foreach (var content in m.Contents)
            {
                charCount += content is TextContent tc
                    ? tc.Text?.Length ?? 0
                    : content.ToString()?.Length ?? 0;
            }
            charCount += 1; // newline
        }

        // Standard approximation: ~4 characters per token
        return charCount / 4;
    }

    private static string ExtractText(ChatMessage m)
    {
        var parts = new List<string>();
        foreach (var content in m.Contents)
        {
            if (content is TextContent tc && tc.Text is not null)
                parts.Add(tc.Text);
            else
                parts.Add(content.ToString() ?? string.Empty);
        }
        return string.Join(" ", parts);
    }
}
