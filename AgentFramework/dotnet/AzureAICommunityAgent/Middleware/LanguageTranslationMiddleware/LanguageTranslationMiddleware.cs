using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureAICommunity.Agent.Middleware.LanguageTranslationMiddleware;

/// <summary>
/// A delegating chat client middleware that automatically detects the user's language,
/// translates incoming messages into a configured target language, then back-translates
/// the agent's response into the user's original language.
/// </summary>
/// <remarks>
/// <para>
/// Only the <em>last</em> user message in the conversation is used for language detection;
/// earlier history messages are assumed to already be in the target language.
/// </para>
/// <para>
/// The middleware uses <paramref name="primaryService"/> for detection and translation.
/// If that call fails and <paramref name="fallbackService"/> is supplied, the middleware
/// silently retries with the fallback rather than propagating the error.
/// </para>
/// <para>
/// When the detected language already matches <paramref name="targetLanguage"/>, or when
/// the detection confidence is below <paramref name="minConfidence"/>, the message is
/// forwarded untouched.
/// </para>
/// </remarks>
public sealed class LanguageTranslationMiddleware : DelegatingChatClient
{
    private readonly string _targetLanguage;
    private readonly float _minConfidence;
    private readonly ITranslationService _primaryService;
    private readonly ITranslationService? _fallbackService;
    private readonly ILogger<LanguageTranslationMiddleware> _logger;

    /// <summary>
    /// Creates a <see cref="LanguageTranslationMiddleware"/> instance directly.
    /// For a more ergonomic setup, prefer <see cref="CreateBuilder"/>.
    /// </summary>
    /// <param name="inner">The inner <see cref="IChatClient"/> to delegate to.</param>
    /// <param name="primaryService">
    ///   The primary <see cref="ITranslationService"/> used for detection and translation.
    /// </param>
    /// <param name="targetLanguage">
    ///   ISO 639-1 code of the language the agent should reason in. Defaults to <c>"en"</c>.
    /// </param>
    /// <param name="minConfidence">
    ///   Minimum detection confidence in <c>[0, 1]</c> required to forward a translation.
    ///   Messages with lower confidence are passed through untranslated. Defaults to <c>0.8</c>.
    /// </param>
    /// <param name="fallbackService">
    ///   Optional secondary service tried when <paramref name="primaryService"/> throws.
    /// </param>
    /// <param name="logger">Optional logger. Defaults to a no-op logger.</param>
    public LanguageTranslationMiddleware(
        IChatClient inner,
        ITranslationService primaryService,
        string targetLanguage = "en",
        float minConfidence = 0.8f,
        ITranslationService? fallbackService = null,
        ILogger<LanguageTranslationMiddleware>? logger = null)
        : base(inner)
    {
        _primaryService = primaryService ?? throw new ArgumentNullException(nameof(primaryService));
        _targetLanguage = string.IsNullOrWhiteSpace(targetLanguage)
            ? throw new ArgumentException("Target language must not be empty.", nameof(targetLanguage))
            : targetLanguage;
        _minConfidence = minConfidence is >= 0f and <= 1f
            ? minConfidence
            : throw new ArgumentOutOfRangeException(nameof(minConfidence), "Confidence must be between 0 and 1.");
        _fallbackService = fallbackService;
        _logger = logger ?? NullLogger<LanguageTranslationMiddleware>.Instance;
    }

    /// <summary>Returns a new <see cref="LanguageTranslationMiddlewareBuilder"/> for fluent configuration.</summary>
    public static LanguageTranslationMiddlewareBuilder CreateBuilder() => new();

    // ── Non-streaming ─────────────────────────────────────────────────────── //

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();

        var userLanguage =
            await TranslateLastUserMessageAsync(messageList, cancellationToken).ConfigureAwait(false);

        var response = await base.GetResponseAsync(messageList, options, cancellationToken)
            .ConfigureAwait(false);

        if (userLanguage is not null)
            await BackTranslateResponseAsync(response.Messages, userLanguage, cancellationToken)
                .ConfigureAwait(false);

        return response;
    }

    // ── Streaming ─────────────────────────────────────────────────────────── //

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();

        var userLanguage =
            await TranslateLastUserMessageAsync(messageList, cancellationToken).ConfigureAwait(false);

        // Collect all streaming chunks so we can back-translate the full response.
        var chunks = new List<ChatResponseUpdate>();
        var fullTextBuilder = new StringBuilder();

        await foreach (var update in base
            .GetStreamingResponseAsync(messageList, options, cancellationToken)
            .ConfigureAwait(false))
        {
            chunks.Add(update);
            if (update.Text is { Length: > 0 } text)
                fullTextBuilder.Append(text);
        }

        if (userLanguage is null || fullTextBuilder.Length == 0)
        {
            // No translation needed — forward the original chunks.
            foreach (var chunk in chunks)
                yield return chunk;
            yield break;
        }

        // Back-translate the full response text and emit it as a single chunk followed by non-text chunks.
        var fullText = fullTextBuilder.ToString().Trim();
        string? backTranslated = null;

        try
        {
            backTranslated = await BackTranslateTextAsync(fullText, userLanguage, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Back-translation to '{Language}' failed during streaming; forwarding original response.",
                userLanguage);
        }

        if (backTranslated is not null)
        {
            // Emit the back-translated text and any non-text content from the last chunk.
            var lastChunk = chunks.LastOrDefault();
            var nonTextContents = lastChunk?.Contents
                .Where(c => c is not TextContent)
                .ToList() ?? [];

            var responseContents = new List<AIContent> { new TextContent(backTranslated) };
            responseContents.AddRange(nonTextContents);

            yield return new ChatResponseUpdate(ChatRole.Assistant, responseContents)
            {
                ModelId = lastChunk?.ModelId,
            };
        }
        else
        {
            // Fallback: forward original chunks.
            foreach (var chunk in chunks)
                yield return chunk;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────── //

    /// <summary>
    /// Detects the language of the last user message and, when translation is needed,
    /// mutates the message's text in-place and returns the detected user language code.
    /// </summary>
    private async Task<string?> TranslateLastUserMessageAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var lastUser = messages.LastOrDefault(m => m.Role == ChatRole.User);
        var originalText = lastUser?.Text?.Trim();

        if (lastUser is null || string.IsNullOrEmpty(originalText))
            return null;

        _logger.LogDebug("Detecting language for user message (length: {Length}).", originalText.Length);

        string detectedLanguage;
        float? confidence;

        try
        {
            (detectedLanguage, confidence) = await _primaryService
                .DetectLanguageAsync(originalText, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary service detect_language failed; trying fallback.");
            if (_fallbackService is null)
                return null;

            try
            {
                (detectedLanguage, confidence) = await _fallbackService
                    .DetectLanguageAsync(originalText, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogWarning(fallbackEx, "Fallback service detect_language also failed; passing through untranslated.");
                return null;
            }
        }

        if (detectedLanguage == _targetLanguage)
        {
            _logger.LogDebug("Detected language '{Language}' matches target; no translation needed.", detectedLanguage);
            return null;
        }

        bool confidenceMet = confidence is null || confidence >= _minConfidence;
        if (!confidenceMet)
        {
            _logger.LogInformation(
                "Skipping translation: detected '{Language}' with confidence {Confidence:F2} < threshold {Threshold:F2}.",
                detectedLanguage, confidence, _minConfidence);
            return null;
        }

        // Translate the user message into the target language.
        var translated = await TranslateAsync(originalText, detectedLanguage, _targetLanguage, cancellationToken)
            .ConfigureAwait(false);

        if (translated is null)
            return null;

        // Mutate in-place so the inner client sees the translated text.
        SetMessageText(lastUser, translated);
        _logger.LogDebug("Translated user message from '{Source}' to '{Target}'.", detectedLanguage, _targetLanguage);

        return detectedLanguage;
    }

    /// <summary>Back-translates all assistant messages in <paramref name="responseMessages"/>.</summary>
    private async Task BackTranslateResponseAsync(
        IEnumerable<ChatMessage> responseMessages,
        string userLanguage,
        CancellationToken cancellationToken)
    {
        foreach (var msg in responseMessages)
        {
            var text = msg.Text?.Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            var backTranslated = await BackTranslateTextAsync(text, userLanguage, cancellationToken)
                .ConfigureAwait(false);

            if (backTranslated is not null)
            {
                SetMessageText(msg, backTranslated);
                _logger.LogDebug("Back-translated response to '{Language}' (length: {Length}).",
                    userLanguage, backTranslated.Length);
            }
        }
    }

    private async Task<string?> BackTranslateTextAsync(
        string text,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _primaryService
                .TranslateAsync(text, _targetLanguage, targetLanguage, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary service back-translation to '{Language}' failed; trying fallback.", targetLanguage);

            if (_fallbackService is null)
                return null;

            try
            {
                return await _fallbackService
                    .TranslateAsync(text, _targetLanguage, targetLanguage, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogWarning(fallbackEx, "Fallback back-translation to '{Language}' also failed.", targetLanguage);
                return null;
            }
        }
    }

    private async Task<string?> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _primaryService
                .TranslateAsync(text, sourceLanguage, targetLanguage, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary translation from '{Source}' to '{Target}' failed; trying fallback.", sourceLanguage, targetLanguage);

            if (_fallbackService is null)
                return null;

            try
            {
                return await _fallbackService
                    .TranslateAsync(text, sourceLanguage, targetLanguage, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogWarning(fallbackEx, "Fallback translation from '{Source}' to '{Target}' also failed.", sourceLanguage, targetLanguage);
                return null;
            }
        }
    }

    /// <summary>Replaces the text of <paramref name="message"/> using its <see cref="ChatMessage.Contents"/>.</summary>
    private static void SetMessageText(ChatMessage message, string newText)
    {
        var textContent = message.Contents.OfType<TextContent>().FirstOrDefault();
        if (textContent is not null)
            textContent.Text = newText;
    }
}
