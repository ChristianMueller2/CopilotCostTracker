// ViewModels/MainViewModel.cs
// Framework-agnostic: no Microsoft.Maui.* usings allowed in this file.
using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotCostTracker.Models;
using CopilotCostTracker.Services;

namespace CopilotCostTracker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISessionParserService _parser;
    private readonly IFolderWatcherService _watcher;
    private readonly IPreferencesService   _prefs;
    private readonly INavigationService    _nav;
    private readonly IUserMessageService   _msg;

    [ObservableProperty] public partial ObservableCollection<CopilotSession> Sessions           { get; set; }
    [ObservableProperty] public partial string                               FilterText          { get; set; }
    [ObservableProperty] public partial string                               ActiveTabFilter     { get; set; }
    [ObservableProperty] public partial bool                                 IsLoading           { get; set; }
    [ObservableProperty] public partial string                               LastUpdated         { get; set; }
    [ObservableProperty] public partial bool                                 ShowTokens          { get; set; }
    [ObservableProperty] public partial bool                                 EnableEclipseEstimation { get; set; }
    [ObservableProperty] public partial bool                                 ShowTokenInfoHint   { get; set; }

    // Computed summary fields (updated in RecalcSummary)
    public int     TotalSessionCount      { get; private set; }
    public int     TotalFolderCount       { get; private set; }
    public long    TotalInputTokens       { get; private set; }
    public long    TotalOutputTokens      { get; private set; }
    public long    TotalCacheReadTokens   { get; private set; }
    public long    TotalCacheWriteTokens  { get; private set; }
    public decimal TotalInputCostUsd      { get; private set; }
    public decimal TotalOutputCostUsd     { get; private set; }
    public decimal TotalCacheReadCostUsd  { get; private set; }
    public decimal TotalCacheWriteCostUsd { get; private set; }
    public decimal GrandTotalCostUsd      { get; private set; }
    public decimal GrandTotalCredits      { get; private set; }
    public decimal CacheSavingsUsd        { get; private set; }

    public decimal PctInput      { get; private set; }
    public decimal PctOutput     { get; private set; }
    public decimal PctCacheRead  { get; private set; }
    public decimal PctCacheWrite { get; private set; }

    public bool IsWatcherActive => WatchedFolders.Count > 0;

    public IReadOnlyList<ModelCostItem>   CostByModel     { get; private set; } = [];
    public IReadOnlyList<ModelCostDetail> CostByModelFull { get; private set; } = [];

    public IReadOnlyList<WatchedFolder> WatchedFolders
        => JsonSerializer.Deserialize(
                _prefs.Get("WatchedFolders", "[]"),
                AppJsonContext.Default.WatchedFolderArray)
           ?? [];

    public IReadOnlyList<CopilotSession> FilteredSessions
    {
        get
        {
            var q = Sessions.AsEnumerable();

            if (ActiveTabFilter == "Today")
                q = q.Where(s => s.StartTime.Date == DateTime.Today);
            else if (ActiveTabFilter == "Week")
                q = q.Where(s => s.StartTime >= DateTime.Today.AddDays(-7));
            else if (ActiveTabFilter == "Month")
                q = q.Where(s => s.StartTime >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var t = FilterText.Trim();
                q = q.Where(s =>
                    s.Repository.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                    s.Model.Contains(t, StringComparison.OrdinalIgnoreCase));
            }

            return q.ToList();
        }
    }

    public MainViewModel(
        ISessionParserService parser,
        IFolderWatcherService watcher,
        IPreferencesService   prefs,
        INavigationService    nav,
        IUserMessageService   msg)
    {
        _parser         = parser;
        _watcher        = watcher;
        _prefs          = prefs;
        _nav            = nav;
        _msg            = msg;
        Sessions        = new ObservableCollection<CopilotSession>();
        FilterText      = string.Empty;
        ActiveTabFilter = "All";
        LastUpdated     = string.Empty;
        EnableEclipseEstimation = _prefs.Get("EnableEclipseEstimation", "false") == "true";
        ShowTokenInfoHint       = _prefs.Get("ShowTokenInfoHint", "true") == "true";

        _watcher.FileChanged += async (_, _) => await RefreshAsync();
    }

    [RelayCommand]
    public void DismissTokenInfoHint()
    {
        ShowTokenInfoHint = false;
        _prefs.Set("ShowTokenInfoHint", "false");
    }

    [RelayCommand]
    public async Task ShowEclipseInfoAsync()
    {
        await _msg.ShowAlertAsync(
            "Eclipse-Sch\u00e4tzung",
            "Token-Kosten f\u00fcr Eclipse-Sessions werden gesch\u00e4tzt (Zeichen \u00f7 4), " +
            "da Eclipse keine echten Token-Zahlen speichert. " +
            "Die Kosten sind N\u00e4herungswerte.",
            "OK");
    }

    [RelayCommand]
    public Task GoToFoldersAsync() => _nav.GoToFoldersAsync();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var folders = WatchedFolders;
            _watcher.SetFolders(folders);
            var results = await _parser.ParseAllFoldersAsync(folders);
            Sessions = new ObservableCollection<CopilotSession>(results);
            RecalcSummary();
            LastUpdated = $"Updated {DateTime.Now:HH:mm}";
        }
        finally
        {
            IsLoading = false;
        }
        OnPropertyChanged(nameof(FilteredSessions));
    }

    [RelayCommand]
    public void SetTabFilter(string filter)
    {
        ActiveTabFilter = filter;
        OnPropertyChanged(nameof(FilteredSessions));
    }

    [RelayCommand]
    public void ClearFilter()
    {
        FilterText = string.Empty;
        OnPropertyChanged(nameof(FilteredSessions));
    }

    partial void OnFilterTextChanged(string value)     { OnPropertyChanged(nameof(FilteredSessions)); RecalcSummary(); }
    partial void OnActiveTabFilterChanged(string value)  { OnPropertyChanged(nameof(FilteredSessions)); RecalcSummary(); }
    partial void OnEnableEclipseEstimationChanged(bool value)
    {
        _prefs.Set("EnableEclipseEstimation", value ? "true" : "false");
        OnPropertyChanged(nameof(FilteredSessions));
        RecalcSummary();
    }

    private void RecalcSummary()
    {
        var allSessions = FilteredSessions;

        // Eclipse estimated sessions are always shown in the list but excluded from
        // cost totals unless the user explicitly enabled the Eclipse estimation switch.
        var view = EnableEclipseEstimation
            ? allSessions
            : allSessions.Where(s => !s.IsEstimated).ToList();

        TotalSessionCount      = allSessions.Count;   // always count all sessions in list
        TotalFolderCount       = WatchedFolders.Count;
        TotalInputTokens       = view.Sum(s => s.Usage.InputTokens);
        TotalOutputTokens      = view.Sum(s => s.Usage.OutputTokens);
        TotalCacheReadTokens   = view.Sum(s => s.Usage.CacheReadTokens);
        TotalCacheWriteTokens  = view.Sum(s => s.Usage.CacheWriteTokens);
        TotalInputCostUsd      = view.Sum(s => s.InputCostUsd);
        TotalOutputCostUsd     = view.Sum(s => s.OutputCostUsd);
        TotalCacheReadCostUsd  = view.Sum(s => s.CacheReadCostUsd);
        TotalCacheWriteCostUsd = view.Sum(s => s.CacheWriteCostUsd);
        GrandTotalCostUsd      = view.Sum(s => s.TotalCostUsd);
        GrandTotalCredits      = view.Sum(s => s.TotalCredits);

        // Cache savings: what it would have cost at full input price minus what was paid for cache reads
        CacheSavingsUsd = view.Sum(s =>
        {
            var fullCost = (s.Usage.CacheReadTokens / 1_000_000m) *
                           (s.InputCostUsd > 0 && s.Usage.InputTokens > 0
                               ? (s.InputCostUsd / s.Usage.InputTokens * 1_000_000m / 0.01m)
                               : 3.00m) * 0.01m;
            return fullCost - s.CacheReadCostUsd;
        });

        var total = GrandTotalCostUsd;
        if (total > 0)
        {
            PctInput      = TotalInputCostUsd      / total;
            PctOutput     = TotalOutputCostUsd     / total;
            PctCacheRead  = TotalCacheReadCostUsd  / total;
            PctCacheWrite = TotalCacheWriteCostUsd / total;
        }
        else
        {
            PctInput = PctOutput = PctCacheRead = PctCacheWrite = 0;
        }

        var rawCostByModel = view
            .GroupBy(s => s.Model)
            .Select(g => new ModelCostItem { Model = g.Key, CostUsd = g.Sum(s => s.TotalCostUsd) })
            .OrderByDescending(x => x.CostUsd)
            .Take(5)
            .ToList();
        var maxCost = rawCostByModel.Count > 0 ? (double)rawCostByModel[0].CostUsd : 1.0;
        if (maxCost <= 0) maxCost = 1.0;
        CostByModel = rawCostByModel
            .Select(x => new ModelCostItem
            {
                Model          = x.Model,
                CostUsd        = x.CostUsd,
                BarWidthPixels = (double)x.CostUsd / maxCost * 180.0
            })
            .ToList();

        CostByModelFull = view
            .GroupBy(s => s.Model)
            .Select(g => new ModelCostDetail
            {
                Model             = g.Key,
                TotalCostUsd      = g.Sum(s => s.TotalCostUsd),
                InputTokens       = g.Sum(s => s.Usage.InputTokens),
                OutputTokens      = g.Sum(s => s.Usage.OutputTokens),
                CacheReadTokens   = g.Sum(s => s.Usage.CacheReadTokens),
                CacheWriteTokens  = g.Sum(s => s.Usage.CacheWriteTokens),
                InputCostUsd      = g.Sum(s => s.InputCostUsd),
                OutputCostUsd     = g.Sum(s => s.OutputCostUsd),
                CacheReadCostUsd  = g.Sum(s => s.CacheReadCostUsd),
                CacheWriteCostUsd = g.Sum(s => s.CacheWriteCostUsd)
            })
            .OrderByDescending(x => x.TotalCostUsd)
            .ToList();

        OnPropertyChanged(nameof(TotalSessionCount));
        OnPropertyChanged(nameof(TotalFolderCount));
        OnPropertyChanged(nameof(TotalInputTokens));
        OnPropertyChanged(nameof(TotalOutputTokens));
        OnPropertyChanged(nameof(TotalCacheReadTokens));
        OnPropertyChanged(nameof(TotalCacheWriteTokens));
        OnPropertyChanged(nameof(TotalInputCostUsd));
        OnPropertyChanged(nameof(TotalOutputCostUsd));
        OnPropertyChanged(nameof(TotalCacheReadCostUsd));
        OnPropertyChanged(nameof(TotalCacheWriteCostUsd));
        OnPropertyChanged(nameof(GrandTotalCostUsd));
        OnPropertyChanged(nameof(GrandTotalCredits));
        OnPropertyChanged(nameof(CacheSavingsUsd));
        OnPropertyChanged(nameof(PctInput));
        OnPropertyChanged(nameof(PctOutput));
        OnPropertyChanged(nameof(PctCacheRead));
        OnPropertyChanged(nameof(PctCacheWrite));
        OnPropertyChanged(nameof(CostByModel));
        OnPropertyChanged(nameof(CostByModelFull));
        OnPropertyChanged(nameof(WatchedFolders));
        OnPropertyChanged(nameof(IsWatcherActive));
    }
}
