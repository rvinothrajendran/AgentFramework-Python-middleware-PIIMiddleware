namespace AzureAICommunityAgent.Middleware.ToolLimiting
{
    /// <summary>
    /// Exposes tool-call usage information and reset capability from the tool limit middleware.
    /// Retrieve an instance via <c>chatClient.GetService&lt;IToolLimitTracker&gt;()</c>.
    /// </summary>
    public interface IToolLimitTracker
    {
        /// <summary>Returns a snapshot of current tool call counters.</summary>
        ToolUsageState GetCurrentUsage();

        /// <summary>Resets all call counters to zero.</summary>
        void Reset();
    }
}
