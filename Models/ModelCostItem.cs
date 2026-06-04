// Models/ModelCostItem.cs
namespace CopilotCostTracker.Models;

/// <summary>Simple model/cost pair used by the bar chart on MainPage.</summary>
public sealed class ModelCostItem
{
    public string  Model         { get; init; } = string.Empty;
    public decimal CostUsd       { get; init; }
    /// <summary>Pre-computed bar width in device-independent pixels (max 180).</summary>
    public double  BarWidthPixels { get; init; }
}
