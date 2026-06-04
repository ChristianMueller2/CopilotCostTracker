// Models/TokenUsage.cs
namespace CopilotCostTracker.Models;

public class TokenUsage
{
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheReadTokens { get; set; }
    public long CacheWriteTokens { get; set; }
}
