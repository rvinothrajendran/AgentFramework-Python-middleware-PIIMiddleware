using Microsoft.Extensions.AI;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Microsoft.Recognizers.Text.Sequence;

namespace AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware;

/// <summary>
/// A delegating chat client middleware that detects and handles Personally Identifiable Information (PII)
/// in outgoing chat messages based on the configured <see cref="PIIPolicy"/>.
/// </summary>
/// <param name="inner">The inner <see cref="IChatClient"/> to delegate requests to.</param>
/// <param name="allowList">Optional set of PII types to always allow through, bypassing policy enforcement.</param>
/// <param name="blockList">Optional set of PII types to enforce the policy on. If empty, all detected PII types are protected.</param>
/// <param name="policy">The <see cref="PIIPolicy"/> that determines how detected PII is handled (Allow, Block, or Mask).</param>
/// <param name="culture">The culture used by the recognizers for language-specific detection. Defaults to English.</param>
public class PIIChatDetectionMiddleware(
    IChatClient inner,
    IEnumerable<string>? allowList = null,
    IEnumerable<string>? blockList = null,
    PIIPolicy policy = PIIPolicy.Block,
    string culture = Culture.English)
    : DelegatingChatClient(inner)
{
    private readonly HashSet<string> _allowList = allowList?.ToHashSet(System.StringComparer.OrdinalIgnoreCase)
                                                  ?? new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _blockList = blockList?.ToHashSet(System.StringComparer.OrdinalIgnoreCase)
                                                  ?? new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>Represents a single PII detection result with its type, matched text, and character range.</summary>
    private record Detection(string Type, string Text, int Start, int End);

    /// <summary>
    /// Runs all configured recognizers against <paramref name="text"/> and returns every PII span found.
    /// </summary>
    /// <param name="text">The raw input text to scan for PII.</param>
    /// <returns>A list of <see cref="Detection"/> entries, one per recognized PII span.</returns>
    private List<Detection> Detect(string text)
    {
        var detections = new List<Detection>();

        void Add(IEnumerable<ModelResult> results)
        {
            foreach (var r in results)
            {
                if (string.IsNullOrWhiteSpace(r.Text))
                    continue;

                detections.Add(new Detection(r.TypeName, r.Text, r.Start, r.End));
            }
        }

        Add(SequenceRecognizer.RecognizeEmail(text, culture));
        Add(SequenceRecognizer.RecognizePhoneNumber(text, culture));
        Add(SequenceRecognizer.RecognizeIpAddress(text, culture));
        Add(SequenceExtensions.RecognizeCreditCard(text));
        Add(NumberRecognizer.RecognizeNumber(text, culture));
        Add(DateTimeRecognizer.RecognizeDateTime(text, culture));
        Add(NumberWithUnitRecognizer.RecognizeDimension(text, culture));

        return detections;
    }

    /// <summary>
    /// Replaces each detected PII span in <paramref name="text"/> with a typed placeholder token
    /// (e.g. <c>&lt;EMAIL_1&gt;</c>, <c>&lt;PHONE_NUMBER_2&gt;</c>).
    /// Overlapping spans are resolved by keeping the longest match at each position.
    /// </summary>
    /// <param name="text">The original message text.</param>
    /// <param name="detections">The PII detections to mask.</param>
    /// <returns>The text with all PII spans replaced by placeholder tokens.</returns>
    private string MaskText(string text, IEnumerable<Detection> detections)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sorted = detections
            .Where(d => d.Start >= 0 && d.End >= d.Start && d.End < text.Length)
            .OrderBy(d => d.Start)
            .ThenByDescending(d => d.End - d.Start)
            .ToList();

        if (sorted.Count == 0)
            return text;

        // Merge overlapping ranges so masking always covers the furthest overlapping end.
        // When ranges share a start, the sort order still ensures the longest match is considered first.
        // Tie-breaking: when two overlapping detections have equal span length, the first one's type is kept.
        var selected = new List<(int Start, int End, string Type)>();

        var currentStart = sorted[0].Start;
        var currentEnd = sorted[0].End;
        var currentType = sorted[0].Type;
        var currentSpanLength = currentEnd - currentStart;

        foreach (var d in sorted.Skip(1))
        {
            if (d.Start <= currentEnd)
            {
                if (d.End > currentEnd)
                    currentEnd = d.End;

                var detectionSpanLength = d.End - d.Start;
                if (detectionSpanLength > currentSpanLength)
                {
                    currentType = d.Type;
                    currentSpanLength = detectionSpanLength;
                }

                continue;
            }

            selected.Add((currentStart, currentEnd, currentType));
            currentStart = d.Start;
            currentEnd = d.End;
            currentType = d.Type;
            currentSpanLength = d.End - d.Start;
        }

        selected.Add((currentStart, currentEnd, currentType));

        var counters = new Dictionary<string, int>();
        var replacements = new List<(int Start, int Length, string Token)>();

        foreach (var d in selected)
        {
            counters[d.Type] = counters.GetValueOrDefault(d.Type) + 1;

            var tokenType = new string(d.Type
                .Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_')
                .ToArray());

            var token = $"<{tokenType}_{counters[d.Type]}>";
            replacements.Add((d.Start, d.End - d.Start + 1, token));
        }

        // Apply replacements in reverse order so earlier character positions are not shifted
        // by the length difference introduced by later substitutions.
        foreach (var replacement in replacements.OrderByDescending(r => r.Start))
        {
            text = text.Remove(replacement.Start, replacement.Length)
                .Insert(replacement.Start, replacement.Token);
        }

        return text;
    }

    /// <summary>
    /// Intercepts a non-streaming chat request, scans the last user message for PII,
    /// and either passes it through, blocks it, or masks it according to the configured <see cref="PIIPolicy"/>.
    /// </summary>
    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (policy == PIIPolicy.Allow)
            return await base.GetResponseAsync(messages, options, cancellationToken);

        var messageList = messages.ToList();
        var lastUserIndex = messageList.FindLastIndex(m => m.Role == ChatRole.User);

        if (lastUserIndex < 0 || string.IsNullOrEmpty(messageList[lastUserIndex].Text))
            return await base.GetResponseAsync(messageList, options, cancellationToken);

        var last = messageList[lastUserIndex];

        var detections = Detect(last.Text)
            .Where(d => !_allowList.Contains(d.Type))
            .ToList();

        if (detections.Count == 0)
            return await base.GetResponseAsync(messageList, options, cancellationToken);

        // If blockList is empty, protect all detected types; otherwise only those in the blockList
        var protectedDetections = _blockList.Count == 0
            ? detections
            : detections.Where(d => _blockList.Contains(d.Type)).ToList();

        if (protectedDetections.Count == 0)
            return await base.GetResponseAsync(messageList, options, cancellationToken);

        if (policy == PIIPolicy.Block)
        {
            var blockedTypes = protectedDetections
                .Select(b => b.Type)
                .Distinct(System.StringComparer.OrdinalIgnoreCase);

            return new ChatResponse(new[]
            {
                new ChatMessage(
                    ChatRole.Assistant,
                    $"Message blocked due to sensitive data: {string.Join(", ", blockedTypes)}"
                )
            });
        }

        if (policy == PIIPolicy.Mask)
        {
            var maskedText = MaskText(last.Text, protectedDetections);
            var updatedLast = last.Clone();
            updatedLast.Contents.Clear();
            updatedLast.Contents.Add(new TextContent(maskedText));
            messageList[lastUserIndex] = updatedLast;
        }

        return await base.GetResponseAsync(messageList, options, cancellationToken);
    }

    /// <summary>
    /// Intercepts a streaming chat request, scans the last user message for PII,
    /// and either streams it through, emits a single blocked update, or masks it according to the configured <see cref="PIIPolicy"/>.
    /// </summary>
    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (policy == PIIPolicy.Allow)
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
                yield return update;
            yield break;
        }

        var messageList = messages.ToList();
        var lastUserIndex = messageList.FindLastIndex(m => m.Role == ChatRole.User);

        if (lastUserIndex < 0 || string.IsNullOrEmpty(messageList[lastUserIndex].Text))
        {
            await foreach (var update in base.GetStreamingResponseAsync(messageList, options, cancellationToken))
                yield return update;
            yield break;
        }

        var last = messageList[lastUserIndex];

        var detections = Detect(last.Text)
            .Where(d => !_allowList.Contains(d.Type))
            .ToList();

        if (detections.Count == 0)
        {
            await foreach (var update in base.GetStreamingResponseAsync(messageList, options, cancellationToken))
                yield return update;
            yield break;
        }

        // If blockList is empty, protect all detected types; otherwise only those in the blockList
        var protectedDetections = _blockList.Count == 0
            ? detections
            : detections.Where(d => _blockList.Contains(d.Type)).ToList();

        if (protectedDetections.Count == 0)
        {
            await foreach (var update in base.GetStreamingResponseAsync(messageList, options, cancellationToken))
                yield return update;
            yield break;
        }

        if (policy == PIIPolicy.Block)
        {
            var blockedTypes = protectedDetections
                .Select(b => b.Type)
                .Distinct(System.StringComparer.OrdinalIgnoreCase);

            yield return new ChatResponseUpdate(
                ChatRole.Assistant,
                $"Message blocked due to sensitive data: {string.Join(", ", blockedTypes)}"
            );
            yield break;
        }

        if (policy == PIIPolicy.Mask)
        {
            var maskedText = MaskText(last.Text, protectedDetections);
            var updatedLast = last.Clone();
            updatedLast.Contents.Clear();
            updatedLast.Contents.Add(new TextContent(maskedText));
            messageList[lastUserIndex] = updatedLast;
        }

        await foreach (var update in base.GetStreamingResponseAsync(messageList, options, cancellationToken))
            yield return update;
    }
}