// Models/ModelPricing.cs
namespace CopilotCostTracker.Models;

public class ModelPricing
{
    /// <summary>Vendor group shown in the settings table (e.g. "Anthropic", "OpenAI").</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Match key used against log model names (case-insensitive, spaces = hyphens).</summary>
    public string ModelKey { get; set; } = string.Empty;

    public decimal InputPer1M { get; set; }
    public decimal CachedInputPer1M { get; set; }
    public decimal CacheWritePer1M { get; set; }
    public decimal OutputPer1M { get; set; }
}
