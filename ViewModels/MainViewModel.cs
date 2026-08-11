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
    private readonly ITrayService          _tray;
    private readonly INotificationService  _notifications;
    private readonly PricingService        _pricing;

    // Tracks the last day/month for which a limit notification was sent
    private DateTime? _lastDailyNotificationDate;
    private int?      _lastMonthlyNotificationMonth;
    // Models already reported as unknown so we don't repeat the notification
    private readonly HashSet<string> _reportedUnknownModels = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] public partial ObservableCollection<CopilotSession> Sessions           { get; set; }
    [ObservableProperty] public partial string                               FilterText          { get; set; }
    [ObservableProperty] public partial string                               ActiveTabFilter     { get; set; }
    [ObservableProperty] public partial DateTime                             CustomFromDate      { get; set; }
    [ObservableProperty] public partial DateTime                             CustomToDate        { get; set; }
    [ObservableProperty] public partial bool                                 IsLoading           { get; set; }
    [ObservableProperty] public partial string                               LastUpdated         { get; set; }
    [ObservableProperty] public partial bool                                 ShowTokens          { get; set; }
    [ObservableProperty] public partial bool                                 EnableEclipseEstimation { get; set; }
    [ObservableProperty] public partial bool                                 ShowTokenInfoHint   { get; set; }
    [ObservableProperty] public partial bool                                 ShowCredits         { get; set; }

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

    // Derived credit totals (1 credit = $0.01)
    public decimal TotalInputCredits      => TotalInputCostUsd      * 100m;
    public decimal TotalOutputCredits     => TotalOutputCostUsd     * 100m;
    public decimal TotalCacheReadCredits  => TotalCacheReadCostUsd  * 100m;
    public decimal TotalCacheWriteCredits => TotalCacheWriteCostUsd * 100m;

    public decimal PctInput      { get; private set; }
    public decimal PctOutput     { get; private set; }
    public decimal PctCacheRead  { get; private set; }
    public decimal PctCacheWrite { get; private set; }

    public bool IsWatcherActive => WatchedFolders.Count > 0;

    public IReadOnlyList<ModelCostItem>   CostByModel          { get; private set; } = [];
    public IReadOnlyList<ModelCostDetail> CostByModelFull      { get; private set; } = [];
    public IReadOnlyList<DailyChartPoint> CumulativeChartPoints { get; private set; } = [];

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
            else if (ActiveTabFilter == "Custom")
            {
                var from = CustomFromDate.Date;
                var to   = CustomToDate.Date;
                if (from > to) (from, to) = (to, from);
                q = q.Where(s => s.StartTime.Date >= from && s.StartTime.Date <= to);
            }

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var t = FilterText.Trim();
                q = q.Where(s =>
                    s.Repository.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                    s.Model.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                    s.Branch.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                    s.SourceFolder.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                    s.SessionId.Contains(t, StringComparison.OrdinalIgnoreCase));
            }

            return q.ToList();
        }
    }

    public MainViewModel(
        ISessionParserService parser,
        IFolderWatcherService watcher,
        IPreferencesService   prefs,
        INavigationService    nav,
        IUserMessageService   msg,
        ITrayService          tray,
        INotificationService  notifications,
        PricingService        pricing)
    {
        _parser         = parser;
        _watcher        = watcher;
        _prefs          = prefs;
        _nav            = nav;
        _msg            = msg;
        _tray           = tray;
        _notifications  = notifications;
        _pricing        = pricing;

        // First run: seed the well-known default log locations so the app has something
        // to show immediately, instead of requiring the user to open the Folders page first.
        DefaultFolderSeeder.SeedIfMissing(_prefs);

        Sessions        = new ObservableCollection<CopilotSession>();
        FilterText      = string.Empty;
        ActiveTabFilter = "All";
        CustomFromDate  = DateTime.Today.AddDays(-7);
        CustomToDate    = DateTime.Today;
        LastUpdated     = string.Empty;
        ShowCredits             = _prefs.Get("ShowCredits", "false") == "true";
        EnableEclipseEstimation = _prefs.Get("EnableEclipseEstimation", "true") == "true";
        ShowTokenInfoHint       = _prefs.Get("ShowTokenInfoHint", "true") == "true";

        _watcher.FileChanged += async (_, _) => await RefreshAsync();
    }

    partial void OnShowCreditsChanged(bool value)
        => _prefs.Set("ShowCredits", value ? "true" : "false");

    [RelayCommand]
    public void ToggleCredits() => ShowCredits = !ShowCredits;

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
            "Cost Estimation",
            "Some sessions cannot provide exact token counts and are marked \u2248:\n\n" +
            "\u2022 Eclipse \u2014 no token data; message text is estimated (chars \u00f7 4) and modeled " +
            "as a growing conversation, since each turn resends the prior context as input.\n\n" +
            "\u2022 Copilot CLI \u2014 input tokens per turn are not logged by the CLI. " +
            "Costs are estimated from conversation context size at the last compaction point. " +
            "Turns after the final compaction are not accounted for, so the actual bill may be higher " +
            "(typically 20\u201340 % for long sessions). Sessions that never compact fall back to a " +
            "message-length estimate (chars \u00f7 4) using the same growing-context model.\n\n" +
            "Estimated figures are approximate and may differ from what the provider charges.",
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
            _pricing.ClearUnknownModels();
            var folders = WatchedFolders;
            _watcher.SetFolders(folders);
            var results = await _parser.ParseAllFoldersAsync(folders);
            Sessions = new ObservableCollection<CopilotSession>(results);
            RecalcSummary();
            LastUpdated = $"Updated {DateTime.Now:HH:mm}";
            UpdateTray();
            CheckLimitNotifications();
            CheckUnknownModelNotification();
        }
        finally
        {
            IsLoading = false;
        }
        OnPropertyChanged(nameof(FilteredSessions));
    }

    private void UpdateTray()
    {
        var today = Sessions
            .Where(s => s.StartTime.Date == DateTime.Today)
            .Sum(s => s.TotalCostUsd);

        var dailyLimitStr = _prefs.Get("DailyLimitUsd", "");
        decimal.TryParse(dailyLimitStr, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var dailyLimit);

        var total = Sessions.Sum(s => s.TotalCostUsd);
        _tray.UpdateTooltip($"Today ${today:F2} | Total ${total:F2}");
        _tray.UpdateTrayIcon(today, dailyLimit);
    }

    private void CheckLimitNotifications()
    {
        if (_prefs.Get("NotificationsEnabled", "true") != "true") return;

        var today = DateTime.Today;

        // Daily limit check
        var dailyLimitStr = _prefs.Get("DailyLimitUsd", "");
        if (decimal.TryParse(dailyLimitStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var dailyLimit)
            && dailyLimit > 0)
        {
            var todayCost = Sessions
                .Where(s => s.StartTime.Date == today)
                .Sum(s => s.TotalCostUsd);

            if (todayCost >= dailyLimit && _lastDailyNotificationDate != today)
            {
                _lastDailyNotificationDate = today;
                _notifications.Show(
                    "Daily limit reached",
                    $"Today's Copilot cost ${todayCost:F2} has reached your daily limit of ${dailyLimit:F2}.",
                    NotificationSeverity.Warning);
            }
        }

        // Monthly limit check
        var monthlyLimitStr = _prefs.Get("MonthlyLimitUsd", "");
        if (decimal.TryParse(monthlyLimitStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var monthlyLimit)
            && monthlyLimit > 0)
        {
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var monthlyCost = Sessions
                .Where(s => s.StartTime >= thisMonth)
                .Sum(s => s.TotalCostUsd);

            if (monthlyCost >= monthlyLimit && _lastMonthlyNotificationMonth != today.Month)
            {
                _lastMonthlyNotificationMonth = today.Month;
                _notifications.Show(
                    "Monthly limit reached",
                    $"This month's Copilot cost ${monthlyCost:F2} has reached your monthly limit of ${monthlyLimit:F2}.",
                    NotificationSeverity.Warning);
            }
        }
    }

    private void CheckUnknownModelNotification()
    {
        if (_prefs.Get("NotificationsEnabled", "true") != "true") return;

        // Only notify about models not yet reported this app session.
        var newUnknown = _pricing.UnknownModels
            .Where(m => _reportedUnknownModels.Add(m))
            .ToList();

        if (newUnknown.Count == 0) return;

        var list    = string.Join(", ", newUnknown.Select(m => $"'{m}'"));
        var message = newUnknown.Count == 1
            ? $"Model {list} has no pricing entry — falling back to default rates. Add it in Settings → Model pricing."
            : $"Models {list} have no pricing entries — falling back to default rates. Add them in Settings → Model pricing.";

        _notifications.Show("Unknown model pricing", message, NotificationSeverity.Warning);
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
    partial void OnCustomFromDateChanged(DateTime value)
    {
        if (ActiveTabFilter == "Custom") { OnPropertyChanged(nameof(FilteredSessions)); RecalcSummary(); }
    }
    partial void OnCustomToDateChanged(DateTime value)
    {
        if (ActiveTabFilter == "Custom") { OnPropertyChanged(nameof(FilteredSessions)); RecalcSummary(); }
    }
    partial void OnEnableEclipseEstimationChanged(bool value)
    {
        _prefs.Set("EnableEclipseEstimation", value ? "true" : "false");
        OnPropertyChanged(nameof(FilteredSessions));
        RecalcSummary();
    }

    private void RecalcSummary()
    {
        var allSessions = FilteredSessions;

        // Estimated sessions (Eclipse, Copilot CLI, …) are included by default;
        // the user can exclude them via the estimation toggle.
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
        OnPropertyChanged(nameof(TotalInputCredits));
        OnPropertyChanged(nameof(TotalOutputCredits));
        OnPropertyChanged(nameof(TotalCacheReadCredits));
        OnPropertyChanged(nameof(TotalCacheWriteCredits));
        OnPropertyChanged(nameof(GrandTotalCostUsd));
        OnPropertyChanged(nameof(GrandTotalCredits));
        OnPropertyChanged(nameof(CacheSavingsUsd));
        OnPropertyChanged(nameof(PctInput));
        OnPropertyChanged(nameof(PctOutput));
        OnPropertyChanged(nameof(PctCacheRead));
        OnPropertyChanged(nameof(PctCacheWrite));
        OnPropertyChanged(nameof(CostByModel));
        OnPropertyChanged(nameof(CostByModelFull));

        // Build cumulative daily chart data
        long    cumTokens = 0;
        decimal cumCost   = 0m;
        CumulativeChartPoints = view
            .GroupBy(s => s.StartTime.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date   = g.Key,
                Tokens = g.Sum(s => s.Usage.InputTokens + s.Usage.OutputTokens
                                  + s.Usage.CacheReadTokens + s.Usage.CacheWriteTokens),
                Cost   = g.Sum(s => s.TotalCostUsd)
            })
            .Select(d =>
            {
                cumTokens += d.Tokens;
                cumCost   += d.Cost;
                return new DailyChartPoint(d.Date, cumTokens, cumCost);
            })
            .ToList();
        OnPropertyChanged(nameof(CumulativeChartPoints));

        OnPropertyChanged(nameof(WatchedFolders));
        OnPropertyChanged(nameof(IsWatcherActive));
    }
}

public record DailyChartPoint(DateTime Date, long CumulativeTokens, decimal CumulativeCostUsd);
