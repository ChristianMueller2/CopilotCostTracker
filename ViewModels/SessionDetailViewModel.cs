// ViewModels/SessionDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotCostTracker.Models;
using CopilotCostTracker.Services;
using System.Collections.ObjectModel;

namespace CopilotCostTracker.ViewModels;

[QueryProperty(nameof(FilePath), "filePath")]
public partial class SessionDetailViewModel : ObservableObject
{
    private readonly ISessionParserService _parser;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _title = "Session";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredEvents))]
    private string _filterText = string.Empty;

    public ObservableCollection<SessionEvent> Events { get; } = [];

    public IEnumerable<SessionEvent> FilteredEvents
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FilterText))
                return Events;
            var q = FilterText.Trim();
            return Events.Where(e =>
                e.Type.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Summary.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Detail.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
    }

    public SessionDetailViewModel(ISessionParserService parser, INavigationService navigation)
    {
        _parser     = parser;
        _navigation = navigation;
    }

    partial void OnFilePathChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _ = LoadAsync(value);
    }

    private async Task LoadAsync(string filePath)
    {
        IsLoading = true;
        Events.Clear();

        Title = Path.GetFileName(Path.GetDirectoryName(filePath) ?? filePath);

        try
        {
            var rawEvents = await _parser.ParseRawEventsAsync(filePath);
            foreach (var e in rawEvents)
                Events.Add(e);
            OnPropertyChanged(nameof(FilteredEvents));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task GoBackAsync() => _navigation.GoBackAsync();
}
