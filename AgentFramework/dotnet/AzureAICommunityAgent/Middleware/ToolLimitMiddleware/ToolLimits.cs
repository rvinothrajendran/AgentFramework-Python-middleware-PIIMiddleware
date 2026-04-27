namespace AzureAICommunityAgent.Middleware.ToolLimiting;

public class ToolLimits
{
    public int GlobalMax { get; set; } = 10;
    public Dictionary<string, int> PerToolMax { get; set; } = new();
}