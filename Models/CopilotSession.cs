// Models/CopilotSession.cs
namespace CopilotCostTracker.Models;

public class CopilotSession
{
    public string SessionId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public long DurationMs { get; set; }
    public int RequestCount { get; set; }
    public TokenUsage Usage { get; set; } = new();
    public string SourceFile { get; set; } = string.Empty;
    public string SourceFolder { get; set; } = string.Empty;

    /// <summary>Token counts are estimated (e.g. chars÷4), not reported by the API.</summary>
    public bool IsEstimated { get; set; }

    public decimal InputCostUsd { get; set; }
    public decimal OutputCostUsd { get; set; }
    public decimal CacheReadCostUsd { get; set; }
    public decimal CacheWriteCostUsd { get; set; }
    public decimal TotalCostUsd => InputCostUsd + OutputCostUsd + CacheReadCostUsd + CacheWriteCostUsd;

    public decimal InputCredits { get; set; }
    public decimal OutputCredits { get; set; }
    public decimal CacheReadCredits { get; set; }
    public decimal CacheWriteCredits { get; set; }
    public decimal TotalCredits => InputCredits + OutputCredits + CacheReadCredits + CacheWriteCredits;
}
