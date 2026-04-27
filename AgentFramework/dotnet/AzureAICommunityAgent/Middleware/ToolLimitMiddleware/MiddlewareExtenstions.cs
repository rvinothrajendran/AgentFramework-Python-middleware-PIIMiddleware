
using AzureAICommunityAgent.Middleware.ToolLimiting;
using Microsoft.Extensions.AI;

namespace AzureAICommunityAgent.Middleware.ToolLimiting
{
    /// <summary>
    /// Middleware to enforce limits on tool usage in a chat client.
    /// </summary>
    public static class ToolLimitMiddlewareExtensions
    {
        public static ChatClientBuilder UseToolLimit(
            this ChatClientBuilder builder,
            ToolLimits? limits = null)
            => builder.Use(inner => new ToolLimitMiddleware(inner, limits));
    }
}
