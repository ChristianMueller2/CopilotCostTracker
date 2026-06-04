// Models/ModelPricing.cs
namespace CopilotCostTracker.Models;

public class ModelPricing
{
    public string ModelKey { get; set; } = string.Empty;
    public decimal InputPer1M { get; set; }
    public decimal CachedInputPer1M { get; set; }
    public decimal CacheWritePer1M { get; set; }
    public decimal OutputPer1M { get; set; }
}
