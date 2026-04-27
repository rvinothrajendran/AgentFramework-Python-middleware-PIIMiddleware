namespace AzureAICommunityAgent.Middleware.ToolLimiting;

public class ToolUsageState
{
    /// <summary>Total tool calls that were allowed through.</summary>
    public int TotalCalls { get; set; }
    /// <summary>Configured global maximum.</summary>
    public int GlobalLimit { get; set; }
    /// <summary>All attempted calls per tool, including blocked ones.</summary>
    public Dictionary<string, int> PerTool { get; set; } = new();
    /// <summary>Calls that were actually allowed per tool.</summary>
    public Dictionary<string, int> PerToolAllowed { get; set; } = new();
    /// <summary>Configured per-tool limits.</summary>
    public Dictionary<string, int> PerToolLimits { get; set; } = new();
}