using AzureAICommunity.Agent.Middleware.PIIChatDetectionMiddleware;
using Microsoft.Extensions.AI;

namespace AzureAICommunity.Agent.Tests.Middleware.PIIChatDetectionMiddlewareTests;

[TestClass]
public class PIIChatDetectionMiddlewareTests
{
    #region Fake inner client

    private sealed class FakeChatClient : IChatClient
    {
        public List<List<ChatMessage>> Calls { get; } = new();
        public ChatClientMetadata Metadata { get; } = new("fake");

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(messages.ToList());
            return Task.FromResult(new ChatResponse(new[]
            {
                new ChatMessage(ChatRole.Assistant, "ok")
            }));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls.Add(messages.ToList());
            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
            await Task.CompletedTask;
        }

        public TService? GetService<TService>(object? key = null) where TService : class => null;
        public object? GetService(Type serviceType, object? key = null) => null;
        public void Dispose() { }
    }

    #endregion

    #region PIIPolicy.Allow

    [TestMethod]
    public async Task Allow_Policy_PassesMessageThrough_WithoutModification()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Allow);
        var messages = new[] { new ChatMessage(ChatRole.User, "My email is test@example.com") };

        await sut.GetResponseAsync(messages, null);

        Assert.HasCount(1, inner.Calls);
        Assert.AreEqual("My email is test@example.com", inner.Calls[0].Last().Text);
    }

    [TestMethod]
    public async Task Allow_Policy_NoPIIDetection_InnerClientAlwaysCalled()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Allow);
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello, how are you?") };

        await sut.GetResponseAsync(messages, null);

        Assert.HasCount(1, inner.Calls);
    }

    #endregion

    #region PIIPolicy.Mask

    [TestMethod]
    public async Task Mask_Policy_MasksEmailInLastMessage()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask);
        var messages = new[] { new ChatMessage(ChatRole.User, "My email is test@example.com") };

        await sut.GetResponseAsync(messages, null);

        var sentText = inner.Calls[0].Last().Text;
        Assert.DoesNotContain("test@example.com", sentText!, "Email should be masked");
        Assert.Contains("<", sentText, "Masked token should be present");
    }

    [TestMethod]
    public async Task Mask_Policy_MasksPhoneNumberInLastMessage()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask);
        var messages = new[] { new ChatMessage(ChatRole.User, "Call me at +1-800-555-0100") };

        await sut.GetResponseAsync(messages, null);

        var sentText = inner.Calls[0].Last().Text;
        Assert.DoesNotContain("+1-800-555-0100", sentText!, "Phone number should be masked");
    }

    [TestMethod]
    public async Task Mask_Policy_NoPII_PassesMessageUnchanged()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask);
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello, how are you?") };

        await sut.GetResponseAsync(messages, null);

        Assert.AreEqual("Hello, how are you?", inner.Calls[0].Last().Text);
    }

    [TestMethod]
    public async Task Mask_Policy_OnlyLastMessageIsMasked()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask);
        var messages = new[]
        {
            new ChatMessage(ChatRole.User, "My email is first@example.com"),
            new ChatMessage(ChatRole.Assistant, "Got it"),
            new ChatMessage(ChatRole.User, "My email is last@example.com")
        };

        await sut.GetResponseAsync(messages, null);

        var sentMessages = inner.Calls[0];
        Assert.AreEqual("My email is first@example.com", sentMessages[0].Text, "First message should not be masked");
        Assert.DoesNotContain("last@example.com", sentMessages[2].Text!, "Last message should be masked");
    }

    #endregion

    #region PIIPolicy.Block

    [TestMethod]
    public async Task Block_Policy_ReturnsBlockedResponse_WhenPIIDetected()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Block);
        var messages = new[] { new ChatMessage(ChatRole.User, "My email is test@example.com") };

        var response = await sut.GetResponseAsync(messages, null);

        Assert.IsEmpty(inner.Calls, "Inner client should not be called when blocked");
        Assert.Contains("blocked", response.Messages[0].Text!, "Response should indicate message was blocked");
    }

    [TestMethod]
    public async Task Block_Policy_NoPII_PassesThrough()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Block);
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello, how are you?") };

        await sut.GetResponseAsync(messages, null);

        Assert.HasCount(1, inner.Calls, "Inner client should be called when no PII detected");
    }

    #endregion

    #region Edge cases

    [TestMethod]
    public async Task EmptyText_PassesThrough_WithoutError()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask);
        var messages = new[] { new ChatMessage(ChatRole.User, string.Empty) };

        await sut.GetResponseAsync(messages, null);

        Assert.HasCount(1, inner.Calls);
    }

    [TestMethod]
    public async Task NoMessages_PassesThrough_WithoutError()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask);

        await sut.GetResponseAsync(Array.Empty<ChatMessage>(), null);

        Assert.HasCount(1, inner.Calls);
    }

    #endregion

    #region AllowList

    [TestMethod]
    public async Task AllowList_ExcludesTypeFromMasking()
    {
        var inner = new FakeChatClient();
        // "email" is the TypeName returned by Microsoft.Recognizers.Text.Sequence for emails
        var sut = new PIIChatDetectionMiddleware(inner, allowList: new[] { "email" }, policy: PIIPolicy.Mask);
        var messages = new[] { new ChatMessage(ChatRole.User, "My email is test@example.com") };

        await sut.GetResponseAsync(messages, null);

        var sentText = inner.Calls[0].Last().Text;
        Assert.AreEqual("My email is test@example.com", sentText, "Allowed type should not be masked");
    }

    #endregion

    #region BlockList

    [TestMethod]
    public async Task BlockList_OnlyMasksListedTypes()
    {
        var inner = new FakeChatClient();
        // Only mask phone numbers; emails should pass through
        var sut = new PIIChatDetectionMiddleware(inner, blockList: new[] { "phonenumber" }, policy: PIIPolicy.Mask);
        var messages = new[] { new ChatMessage(ChatRole.User, "Email: test@example.com. Phone: +1-800-555-0100") };

        await sut.GetResponseAsync(messages, null);

        var sentText = inner.Calls[0].Last().Text!;
        Assert.Contains("test@example.com", sentText, "Email should not be masked when not in block list");
        Assert.DoesNotContain("+1-800-555-0100", sentText, "Phone should be masked when in block list");
    }

    #endregion

    #region Streaming — GetStreamingResponseAsync

    [TestMethod]
    public async Task Streaming_Allow_Policy_PassesMessageThrough()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Allow);
        var messages = new[] { new ChatMessage(ChatRole.User, "My email is test@example.com") };

        await foreach (var _ in sut.GetStreamingResponseAsync(messages, null)) { }

        Assert.HasCount(1, inner.Calls);
        Assert.AreEqual("My email is test@example.com", inner.Calls[0].Last().Text);
    }

    [TestMethod]
    public async Task Streaming_Mask_Policy_MasksEmailInLastMessage()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask);
        var messages = new[] { new ChatMessage(ChatRole.User, "My email is test@example.com") };

        await foreach (var _ in sut.GetStreamingResponseAsync(messages, null)) { }

        var sentText = inner.Calls[0].Last().Text;
        Assert.DoesNotContain("test@example.com", sentText!, "Email should be masked in streaming");
    }

    [TestMethod]
    public async Task Streaming_Block_Policy_YieldsBlockedUpdate_AndSkipsInnerClient()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Block);
        var messages = new[] { new ChatMessage(ChatRole.User, "My email is test@example.com") };

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in sut.GetStreamingResponseAsync(messages, null))
            updates.Add(update);

        Assert.IsEmpty(inner.Calls, "Inner client should not be called when blocked");
        Assert.Contains("blocked", updates[0].Text!, "Streaming should yield blocked message");
    }

    [TestMethod]
    public async Task Streaming_NoPII_PassesThrough()
    {
        var inner = new FakeChatClient();
        var sut = new PIIChatDetectionMiddleware(inner, policy: PIIPolicy.Mask);
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello, how are you?") };

        await foreach (var _ in sut.GetStreamingResponseAsync(messages, null)) { }

        Assert.HasCount(1, inner.Calls);
        Assert.AreEqual("Hello, how are you?", inner.Calls[0].Last().Text);
    }

    #endregion
}
