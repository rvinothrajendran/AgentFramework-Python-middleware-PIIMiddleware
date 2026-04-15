using AzureAICommunity.Agent.Middleware.TokenUsageMiddleware;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Tests.Middleware.TokenUsageMiddlewareTests;

[TestClass]
public class TokenUsageMiddlewareTests
{
    #region Fake inner client

    private sealed class FakeChatClient : IChatClient
    {
        public List<List<ChatMessage>> Calls { get; } = new();
        public ChatClientMetadata Metadata { get; } = new("fake");

        /// <summary>Usage to attach to every response. Null means no usage reported.</summary>
        public UsageDetails? ReportedUsage { get; set; } = new() { InputTokenCount = 10, OutputTokenCount = 20, TotalTokenCount = 30 };

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(messages.ToList());
            var response = new ChatResponse(new[] { new ChatMessage(ChatRole.Assistant, "ok") })
            {
                Usage = ReportedUsage
            };
            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls.Add(messages.ToList());
            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
            // Final update embeds usage via UsageContent (MEAi 10.x streaming pattern).
            if (ReportedUsage is not null)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant,
                    new List<AIContent> { new UsageContent(ReportedUsage) });
            }
            await Task.CompletedTask;
        }

        public TService? GetService<TService>(object? key = null) where TService : class => null;
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }

    #endregion

    #region Construction

    [TestMethod]
    public void Constructor_ThrowsOnNullQuotaStore()
    {
        var inner = new FakeChatClient();
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new TokenUsageMiddleware(inner, null!, 100));
    }

    [TestMethod]
    public void Constructor_ThrowsOnZeroQuota()
    {
        var inner = new FakeChatClient();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new TokenUsageMiddleware(inner, new InMemoryQuotaStore(), 0));
    }

    [TestMethod]
    public void Constructor_ThrowsOnNegativeQuota()
    {
        var inner = new FakeChatClient();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new TokenUsageMiddleware(inner, new InMemoryQuotaStore(), -1));
    }

    #endregion

    #region Normal completion — usage tracking

    [TestMethod]
    public async Task GetResponseAsync_InnerClientIsCalled()
    {
        var inner = new FakeChatClient();
        var sut = new TokenUsageMiddleware(inner, new InMemoryQuotaStore(), 1000);
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };

        await sut.GetResponseAsync(messages);

        Assert.HasCount(1, inner.Calls);
    }

    [TestMethod]
    public async Task GetResponseAsync_UsageIsRecordedInStore()
    {
        var periodKey = PeriodKeys.Month();
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { TotalTokenCount = 50 } };
        var sut = new TokenUsageMiddleware(inner, store, 1000, periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "vinoth" } };

        await sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }, options);

        Assert.AreEqual(50L, store.GetUsage("vinoth", periodKey));
    }

    [TestMethod]
    public async Task GetResponseAsync_OnUsageCallback_IsInvokedWithCorrectRecord()
    {
        TokenUsageRecord? captured = null;
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { InputTokenCount = 10, OutputTokenCount = 20, TotalTokenCount = 30 } };
        var sut = new TokenUsageMiddleware(
            inner, store, 1000,
            onUsage: (r, _) => { captured = r; return Task.CompletedTask; });
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "Rajendran" } };

        await sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }, options);

        Assert.IsNotNull(captured);
        Assert.AreEqual("Rajendran", captured.UserId);
        Assert.AreEqual(30L, captured.TotalTokens);
        Assert.AreEqual(10L, captured.InputTokens);
        Assert.AreEqual(20L, captured.OutputTokens);
        Assert.AreEqual(1000L, captured.QuotaTokens);
        Assert.AreEqual(30L, captured.UsedTokensAfterCall);
        Assert.IsFalse(captured.IsStreaming);
    }

    [TestMethod]
    public async Task GetResponseAsync_NoUsageReported_DoesNotRecordOrCallOnUsage()
    {
        var periodKey = PeriodKeys.Month();
        var callbackInvoked = false;
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = null };
        var sut = new TokenUsageMiddleware(
            inner, store, 1000,
            onUsage: (_, _) => { callbackInvoked = true; return Task.CompletedTask; },
            periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "carol" } };

        await sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }, options);

        Assert.AreEqual(0L, store.GetUsage("carol", periodKey));
        Assert.IsFalse(callbackInvoked);
    }

    [TestMethod]
    public async Task GetResponseAsync_TotalFallsBackToInputPlusOutput_WhenTotalIsNull()
    {
        var periodKey = PeriodKeys.Month();
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { InputTokenCount = 15, OutputTokenCount = 25, TotalTokenCount = null } };
        var sut = new TokenUsageMiddleware(inner, store, 1000, periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "dave" } };

        await sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }, options);

        Assert.AreEqual(40L, store.GetUsage("dave", periodKey));
    }

    #endregion

    #region Quota enforcement

    [TestMethod]
    public async Task GetResponseAsync_ThrowsQuotaExceedException_WhenQuotaExhausted()
    {
        const string periodKey = "2026-04";
        var store = new InMemoryQuotaStore();
        store.AddUsage("Vinoth", periodKey, 100); // fully consumed
        var inner = new FakeChatClient();
        var sut = new TokenUsageMiddleware(inner, store, 100, periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "Vinoth" } };

        await Assert.ThrowsExactlyAsync<QuotaExceededException>(() =>
            sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }, options));

        Assert.HasCount(0, inner.Calls); // inner must NOT be called
    }

    [TestMethod]
    public async Task GetResponseAsync_OnQuotaExceededCallback_IsFiredBeforeException()
    {
        const string periodKey = "2026-04";
        QuotaExceededInfo? captured = null;
        var store = new InMemoryQuotaStore();
        store.AddUsage("vinoth", periodKey, 200);
        var inner = new FakeChatClient();
        var sut = new TokenUsageMiddleware(
            inner, store, 200,
            onQuotaExceeded: (info, _) => { captured = info; return Task.CompletedTask; },
            periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "vinoth" } };

        await Assert.ThrowsExactlyAsync<QuotaExceededException>(() =>
            sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }, options));

        Assert.IsNotNull(captured);
        Assert.AreEqual("vinoth", captured.UserId);
        Assert.AreEqual(200L, captured.UsedTokens);
        Assert.AreEqual(200L, captured.QuotaTokens);
    }

    [TestMethod]
    public async Task GetResponseAsync_UsageAccumulatesAcrossCalls()
    {
        const string periodKey = "2026-04";
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { TotalTokenCount = 40 } };
        var sut = new TokenUsageMiddleware(inner, store, 1000, periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "eve" } };
        var messages = new[] { new ChatMessage(ChatRole.User, "Hi") };

        await sut.GetResponseAsync(messages, options);
        await sut.GetResponseAsync(messages, options);
        await sut.GetResponseAsync(messages, options);

        Assert.AreEqual(120L, store.GetUsage("eve", periodKey));
    }

    [TestMethod]
    public async Task GetResponseAsync_DifferentUsers_TrackedIndependently()
    {
        const string periodKey = "2026-04";
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { TotalTokenCount = 50 } };
        var sut = new TokenUsageMiddleware(inner, store, 1000, periodKeyFn: () => periodKey);
        var optionsAlice = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "vinoth" } };
        var optionsBob   = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "Rajendran" } };
        var messages = new[] { new ChatMessage(ChatRole.User, "Hi") };

        await sut.GetResponseAsync(messages, optionsAlice);
        await sut.GetResponseAsync(messages, optionsBob);
        await sut.GetResponseAsync(messages, optionsAlice);

        Assert.AreEqual(100L, store.GetUsage("vinoth", periodKey));
        Assert.AreEqual(50L,  store.GetUsage("Rajendran",   periodKey));
    }

    #endregion

    #region User ID extraction

    [TestMethod]
    public async Task GetResponseAsync_DefaultsToAnonymous_WhenNoUserIdInOptions()
    {
        const string periodKey = "2026-04";
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { TotalTokenCount = 10 } };
        var sut = new TokenUsageMiddleware(inner, store, 1000, periodKeyFn: () => periodKey);

        await sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }, null);

        Assert.AreEqual(10L, store.GetUsage("anonymous", periodKey));
    }

    [TestMethod]
    public async Task GetResponseAsync_CustomUserIdGetter_IsUsed()
    {
        const string periodKey = "2026-04";
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { TotalTokenCount = 5 } };
        var sut = new TokenUsageMiddleware(
            inner, store, 1000,
            userIdGetter: (_, _) => "custom-user",
            periodKeyFn: () => periodKey);

        await sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") });

        Assert.AreEqual(5L, store.GetUsage("custom-user", periodKey));
    }

    #endregion

    #region Custom period key

    [TestMethod]
    public async Task GetResponseAsync_CustomPeriodKeyFn_IsUsed()
    {
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { TotalTokenCount = 7 } };
        var sut = new TokenUsageMiddleware(
            inner, store, 1000,
            periodKeyFn: () => "custom-period");
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "vinoth" } };

        await sut.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "Hi") }, options);

        Assert.AreEqual(7L, store.GetUsage("vinoth", "custom-period"));
        Assert.AreEqual(0L, store.GetUsage("vinoth", PeriodKeys.Month()));
    }

    #endregion

    #region Streaming

    [TestMethod]
    public async Task GetStreamingResponseAsync_UsageIsRecorded()
    {
        const string periodKey = "2026-04";
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { TotalTokenCount = 30 } };
        var sut = new TokenUsageMiddleware(inner, store, 1000, periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "vinoth" } };
        var messages = new[] { new ChatMessage(ChatRole.User, "Hi") };

        await foreach (var _ in sut.GetStreamingResponseAsync(messages, options)) { }

        Assert.AreEqual(30L, store.GetUsage("vinoth", periodKey));
    }

    [TestMethod]
    public async Task GetStreamingResponseAsync_IsStreaming_MarkedTrue_OnUsageRecord()
    {
        const string periodKey = "2026-04";
        TokenUsageRecord? captured = null;
        var store = new InMemoryQuotaStore();
        var inner = new FakeChatClient { ReportedUsage = new() { TotalTokenCount = 30 } };
        var sut = new TokenUsageMiddleware(
            inner, store, 1000,
            onUsage: (r, _) => { captured = r; return Task.CompletedTask; },
            periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "vinoth" } };
        var messages = new[] { new ChatMessage(ChatRole.User, "Hi") };

        await foreach (var _ in sut.GetStreamingResponseAsync(messages, options)) { }

        Assert.IsNotNull(captured);
        Assert.IsTrue(captured.IsStreaming);
    }

    [TestMethod]
    public async Task GetStreamingResponseAsync_ThrowsQuotaExceededException_WhenQuotaExhausted()
    {
        const string periodKey = "2026-04";
        var store = new InMemoryQuotaStore();
        store.AddUsage("vinoth", periodKey, 100);
        var inner = new FakeChatClient();
        var sut = new TokenUsageMiddleware(inner, store, 100, periodKeyFn: () => periodKey);
        var options = new ChatOptions { AdditionalProperties = new() { ["user_id"] = "vinoth" } };
        var messages = new[] { new ChatMessage(ChatRole.User, "Hi") };

        await Assert.ThrowsExactlyAsync<QuotaExceededException>(async () =>
        {
            await foreach (var _ in sut.GetStreamingResponseAsync(messages, options)) { }
        });

        Assert.HasCount(0, inner.Calls);
    }

    #endregion

    #region InMemoryQuotaStore

    [TestMethod]
    public void InMemoryQuotaStore_GetUsage_ReturnsZeroForUnknownUser()
    {
        var store = new InMemoryQuotaStore();
        Assert.AreEqual(0L, store.GetUsage("unknown", "2026-04"));
    }

    [TestMethod]
    public void InMemoryQuotaStore_AddUsage_AccumulatesCorrectly()
    {
        var store = new InMemoryQuotaStore();
        store.AddUsage("vinoth", "2026-04", 100);
        store.AddUsage("vinoth", "2026-04", 50);
        Assert.AreEqual(150L, store.GetUsage("vinoth", "2026-04"));
    }

    [TestMethod]
    public void InMemoryQuotaStore_DifferentPeriods_TrackedSeparately()
    {
        var store = new InMemoryQuotaStore();
        store.AddUsage("vinoth", "2026-04", 100);
        store.AddUsage("vinoth", "2026-05", 200);
        Assert.AreEqual(100L, store.GetUsage("vinoth", "2026-04"));
        Assert.AreEqual(200L, store.GetUsage("vinoth", "2026-05"));
    }

    #endregion

    #region PeriodKeys

    [TestMethod]
    public void PeriodKeys_Month_HasCorrectFormat()
    {
        var key = PeriodKeys.Month();
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(key, @"^\d{4}-\d{2}$"),
            $"Unexpected format: {key}");
    }

    [TestMethod]
    public void PeriodKeys_Day_HasCorrectFormat()
    {
        var key = PeriodKeys.Day();
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(key, @"^\d{4}-\d{2}-\d{2}$"),
            $"Unexpected format: {key}");
    }

    [TestMethod]
    public void PeriodKeys_Week_HasCorrectFormat()
    {
        var key = PeriodKeys.Week();
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(key, @"^\d{4}-W\d{2}$"),
            $"Unexpected format: {key}");
    }

    #endregion
}
