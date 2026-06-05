// MainPage.xaml.cs
using CopilotCostTracker.ViewModels;
using CopilotCostTracker.Views;

namespace CopilotCostTracker;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;
    private readonly DonutDrawable    _donut     = new();
    private readonly MiniLineDrawable _miniChart = new();

    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Set donut hole color to match the card background
        if (Application.Current?.Resources.TryGetValue("SurfaceSecondaryColor", out var bg) == true
            && bg is Microsoft.Maui.Graphics.Color bgColor)
            _donut.BackgroundColor = bgColor;

        DonutView.Drawable      = _donut;
        MiniChartView.Drawable  = _miniChart;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.PctInput)
                                or nameof(MainViewModel.GrandTotalCostUsd))
            {
                _donut.PctInput      = (float)vm.PctInput;
                _donut.PctOutput     = (float)vm.PctOutput;
                _donut.PctCacheRead  = (float)vm.PctCacheRead;
                _donut.PctCacheWrite = (float)vm.PctCacheWrite;
                _donut.CenterText    = $"${vm.GrandTotalCostUsd:F2}";
                DonutView.Invalidate();
            }

            if (e.PropertyName is nameof(MainViewModel.CumulativeChartPoints))
            {
                _miniChart.Points = vm.CumulativeChartPoints;
                MiniChartView.Invalidate();
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Sessions.Count == 0)
            await _vm.RefreshCommand.ExecuteAsync(null);
        else
            RefreshCharts();
    }

    private void RefreshCharts()
    {
        _donut.PctInput      = (float)_vm.PctInput;
        _donut.PctOutput     = (float)_vm.PctOutput;
        _donut.PctCacheRead  = (float)_vm.PctCacheRead;
        _donut.PctCacheWrite = (float)_vm.PctCacheWrite;
        _donut.CenterText    = $"${_vm.GrandTotalCostUsd:F2}";
        DonutView.Invalidate();

        _miniChart.Points = _vm.CumulativeChartPoints;
        MiniChartView.Invalidate();
    }
}
