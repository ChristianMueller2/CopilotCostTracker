// ViewModels/PricingRowViewModel.cs
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CopilotCostTracker.Models;

namespace CopilotCostTracker.ViewModels;

/// <summary>Editable row in the model-pricing settings table.</summary>
public partial class PricingRowViewModel : ObservableObject
{
    [ObservableProperty] public partial string Provider        { get; set; } = "";
    [ObservableProperty] public partial string ModelKey        { get; set; } = "";
    [ObservableProperty] public partial string InputPer1M      { get; set; } = "";
    [ObservableProperty] public partial string CachedInputPer1M{ get; set; } = "";
    [ObservableProperty] public partial string CacheWritePer1M { get; set; } = "";
    [ObservableProperty] public partial string OutputPer1M     { get; set; } = "";

    public static PricingRowViewModel FromModel(ModelPricing p) => new()
    {
        Provider         = p.Provider,
        ModelKey         = p.ModelKey,
        InputPer1M       = Fmt(p.InputPer1M),
        CachedInputPer1M = Fmt(p.CachedInputPer1M),
        CacheWritePer1M  = Fmt(p.CacheWritePer1M),
        OutputPer1M      = Fmt(p.OutputPer1M),
    };

    public ModelPricing ToModel() => new()
    {
        Provider         = Provider.Trim(),
        ModelKey         = ModelKey.Trim(),
        InputPer1M       = Parse(InputPer1M),
        CachedInputPer1M = Parse(CachedInputPer1M),
        CacheWritePer1M  = Parse(CacheWritePer1M),
        OutputPer1M      = Parse(OutputPer1M),
    };

    private static string Fmt(decimal v) =>
        v.ToString("0.###", CultureInfo.InvariantCulture);

    private static decimal Parse(string s) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
}
