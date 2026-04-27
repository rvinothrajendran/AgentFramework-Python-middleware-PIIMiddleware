using Microsoft.Extensions.AI;
using System.Collections.Concurrent;

namespace AzureAICommunityAgent.Middleware.ToolLimiting
{
    internal sealed class ToolLimitMiddleware(
        IChatClient innerClient,
        ToolLimits? limits = null)
        : DelegatingChatClient(innerClient), IToolLimitTracker
    {
        private readonly ToolLimits _limits = limits ?? new ToolLimits();
        private readonly object _enforceLock = new();

        private int _totalCalls;
        private readonly ConcurrentDictionary<string, int> _toolCounts = new();
        private readonly ConcurrentDictionary<string, int> _attemptedCounts = new();
        
        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            CancellationToken cancellationToken = default)
        {
            var response = await base.GetResponseAsync(
                messages,
                options,
                cancellationToken);

            InspectToolCalls(response);
            
            return response;
        }

        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            await foreach (var update in base.GetStreamingResponseAsync(
                messages,
                options,
                cancellationToken))
            {
                var removeFunctions = new List<string>();
                InspectContents(update.Contents, removeFunctions);

                if (removeFunctions.Count == 0)
                {
                    yield return update;
                }
            }
        }

        private void InspectToolCalls(ChatResponse response)
        {
            var removeFunctions = new List<string>();
            foreach (var message in response.Messages)
            {
                InspectContents(message.Contents, removeFunctions);
            }

            foreach (var functionName in removeFunctions)
            {
                foreach (var message in response.Messages)
                {
                    var itemsToRemove = message.Contents
                        .OfType<FunctionCallContent>()
                        .Where(c => c.Name == functionName)
                        .Cast<AIContent>()
                        .ToList();

                    foreach (var item in itemsToRemove)
                    {
                        message.Contents.Remove(item);
                    }
                }
            }

            var emptyMessages = response.Messages
                .Where(m => m.Contents.Count == 0)
                .ToList();
            foreach (var empty in emptyMessages)
            {
                response.Messages.Remove(empty);
            }

            if (removeFunctions.Count > 0)
            {
                response.Messages.Add(new ChatMessage
                {
                    Role = ChatRole.User,
                    Contents =
                    {
                        new TextContent("Note: Some tool calls were removed due to limits.")
                    }
                });
            }
        }

        private void InspectContents(IEnumerable<AIContent>? contents, List<string> removeFunctions)
        {
            if (contents is null)
                return;

            foreach (var content in contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    var enforceInfo = Enforce(functionCall.Name);
                    if (!enforceInfo.Item1)
                    {
                        removeFunctions.Add(functionCall.Name);
                    }
                }
            }
        }

        private (bool, string) Enforce(string toolName)
        {
            lock (_enforceLock)
            {
                // Always record the attempt, regardless of whether it is allowed
                _attemptedCounts.AddOrUpdate(toolName, 1, (_, count) => count + 1);

                if (_totalCalls >= _limits.GlobalMax)
                {
                    return (false, $"Global tool call limit reached ({_totalCalls}/{_limits.GlobalMax}).");
                }

                _toolCounts.TryGetValue(toolName, out var existingCount);
                if (_limits.PerToolMax.TryGetValue(toolName, out var max) && existingCount >= max)
                {
                    return (false, $"Tool '{toolName}' call limit reached ({existingCount + 1}/{max}).");
                }

                _toolCounts.AddOrUpdate(toolName, 1, (_, count) => count + 1);
                _totalCalls++;
                return (true, string.Empty);
            }
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _totalCalls, 0);
            _toolCounts.Clear();
            _attemptedCounts.Clear();
        }

        public ToolUsageState GetCurrentUsage() => new()
        {
            TotalCalls = _totalCalls,
            GlobalLimit = _limits.GlobalMax,
            PerTool = new Dictionary<string, int>(_attemptedCounts),
            PerToolAllowed = new Dictionary<string, int>(_toolCounts),
            PerToolLimits = new Dictionary<string, int>(_limits.PerToolMax)
        };

        public override object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType == typeof(IToolLimitTracker)
                ? this
                : base.GetService(serviceType, serviceKey);
    }
}
