// Views/TrendPage.xaml.cs
using CopilotCostTracker.ViewModels;

namespace CopilotCostTracker.Views;

public partial class TrendPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly CumulativeLineDrawable _chart = new();

    public TrendPage(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        ChartView.Drawable = _chart;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.CumulativeChartPoints))
                UpdateChart();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Sessions.Count == 0)
            await _vm.RefreshCommand.ExecuteAsync(null);
        else
            UpdateChart();
    }

    private void UpdateChart()
    {
        _chart.Points = _vm.CumulativeChartPoints;
        ChartView.Invalidate();

        var pts = _vm.CumulativeChartPoints;
        if (pts.Count > 0)
        {
            var last = pts[^1];
            long tokens = last.CumulativeTokens;
            TotalTokensSpan.Text = tokens >= 1_000_000
                ? $"{tokens / 1_000_000.0:F2}M"
                : tokens >= 1_000
                    ? $"{tokens / 1_000.0:F1}K"
                    : tokens.ToString();
            TotalCostSpan.Text = $"${last.CumulativeCostUsd:F4}";
        }
        else
        {
            TotalTokensSpan.Text = "—";
            TotalCostSpan.Text   = "—";
        }
    }
}
