// ViewModels/PricingViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotCostTracker.Services;

namespace CopilotCostTracker.ViewModels;

public partial class PricingViewModel : ObservableObject
{
    private readonly PricingService    _pricing;
    private readonly INavigationService _nav;

    public ObservableCollection<PricingRowViewModel> Rows { get; } = [];

    [ObservableProperty] public partial string StatusMessage { get; set; } = "";

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public PricingViewModel(PricingService pricing, INavigationService nav)
    {
        _pricing = pricing;
        _nav     = nav;
        LoadRows();
    }

    private void LoadRows()
    {
        Rows.Clear();
        foreach (var m in _pricing.GetAll())
            Rows.Add(PricingRowViewModel.FromModel(m));
    }

    [RelayCommand]
    void AddRow() => Rows.Add(new PricingRowViewModel { Provider = "", ModelKey = "" });

    [RelayCommand]
    void DeleteRow(PricingRowViewModel row) => Rows.Remove(row);

    [RelayCommand]
    async Task SaveAsync()
    {
        var models = Rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ModelKey))
            .Select(r => r.ToModel())
            .ToList();
        _pricing.UpdateAll(models);
        await _pricing.SaveAsync();
        StatusMessage = "Saved ✓";
        await Task.Delay(2000);
        StatusMessage = "";
    }

    [RelayCommand]
    async Task ResetToDefaultsAsync()
    {
        _pricing.ResetToDefaults();
        await _pricing.SaveAsync();
        LoadRows();
        StatusMessage = "Reset to defaults ✓";
        await Task.Delay(2000);
        StatusMessage = "";
    }

    [RelayCommand]
    Task GoBackAsync() => _nav.GoBackAsync();

    partial void OnStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasStatus));
}
