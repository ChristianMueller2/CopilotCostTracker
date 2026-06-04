// ViewModels/FolderListViewModel.cs
// Framework-agnostic: no Microsoft.Maui.* usings allowed in this file.
using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotCostTracker.Models;
using CopilotCostTracker.Services;

namespace CopilotCostTracker.ViewModels;

public partial class FolderListViewModel : ObservableObject
{
    private readonly IFolderPickerService  _picker;
    private readonly IFolderWatcherService _watcher;
    private readonly ISessionParserService _parser;
    private readonly IPreferencesService   _prefs;
    private readonly INavigationService    _nav;
    private readonly MainViewModel         _main;

    [ObservableProperty] public partial ObservableCollection<WatchedFolder> WatchedFolders { get; set; }

    // Delegate eclipse toggle to MainViewModel so state is shared
    public bool EnableEclipseEstimation
    {
        get => _main.EnableEclipseEstimation;
        set => _main.EnableEclipseEstimation = value;
    }

    public bool HasNoFolders => WatchedFolders.Count == 0;
    public bool HasFolders   => WatchedFolders.Count > 0;

    public string LastUpdated => _main.LastUpdated;

    public FolderListViewModel(
        IFolderPickerService  picker,
        IFolderWatcherService watcher,
        ISessionParserService parser,
        IPreferencesService   prefs,
        INavigationService    nav,
        MainViewModel         main)
    {
        _picker  = picker;
        _watcher = watcher;
        _parser  = parser;
        _prefs   = prefs;
        _nav     = nav;
        _main    = main;
        WatchedFolders = new ObservableCollection<WatchedFolder>();

        LoadFolders();
        _main.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.LastUpdated))
                OnPropertyChanged(nameof(LastUpdated));
            if (e.PropertyName == nameof(MainViewModel.EnableEclipseEstimation))
                OnPropertyChanged(nameof(EnableEclipseEstimation));
        };
    }

    private void LoadFolders()
    {
        var list = JsonSerializer.Deserialize(
            _prefs.Get("WatchedFolders", "[]"),
            AppJsonContext.Default.WatchedFolderArray) ?? [];
        WatchedFolders = new ObservableCollection<WatchedFolder>(list);
        WatchedFolders.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasNoFolders));
            OnPropertyChanged(nameof(HasFolders));
        };
        OnPropertyChanged(nameof(HasNoFolders));
        OnPropertyChanged(nameof(HasFolders));
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(WatchedFolders.ToArray(), AppJsonContext.Default.WatchedFolderArray);
        _prefs.Set("WatchedFolders", json);
        _watcher.SetFolders(WatchedFolders);
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        var path = await _picker.PickFolderAsync();
        if (string.IsNullOrEmpty(path)) return;
        if (WatchedFolders.Any(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;

        WatchedFolders.Add(new WatchedFolder { Path = path, IncludeSubdirectories = true });
        Persist();
        await _main.RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task RemoveFolderAsync(WatchedFolder folder)
    {
        WatchedFolders.Remove(folder);
        Persist();
        await _main.RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ToggleSubdirsAsync(WatchedFolder folder)
    {
        // The Switch two-way binding already updated folder.IncludeSubdirectories;
        // just persist and refresh.
        Persist();
        await _main.RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private Task GoBackAsync() => _nav.GoBackAsync();
}

