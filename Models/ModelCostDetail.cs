// Models/ModelCostDetail.cs
namespace CopilotCostTracker.Models;

/// <summary>Full per-model cost breakdown used by ByModelPage.</summary>
public sealed class ModelCostDetail
{
    public string  Model            { get; init; } = string.Empty;
    public decimal TotalCostUsd     { get; init; }
    public long    InputTokens      { get; init; }
    public long    OutputTokens     { get; init; }
    public long    CacheReadTokens  { get; init; }
    public long    CacheWriteTokens { get; init; }
    public decimal InputCostUsd     { get; init; }
    public decimal OutputCostUsd    { get; init; }
    public decimal CacheReadCostUsd { get; init; }
    public decimal CacheWriteCostUsd{ get; init; }
}
