using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotCostTracker.Services;

namespace CopilotCostTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IPreferencesService  _prefs;
    private readonly INotificationService _notifications;
    private readonly INavigationService   _nav;
    private readonly IAutostartService    _autostart;

    [ObservableProperty] public partial bool   NotificationsEnabled { get; set; }
    [ObservableProperty] public partial bool   AutostartEnabled     { get; set; }
    [ObservableProperty] public partial string DailyLimitText       { get; set; }
    [ObservableProperty] public partial string MonthlyLimitText     { get; set; }
    [ObservableProperty] public partial string SaveConfirmation      { get; set; }

    public bool HasSaveConfirmation => !string.IsNullOrEmpty(SaveConfirmation);

    public SettingsViewModel(
        IPreferencesService  prefs,
        INotificationService notifications,
        INavigationService   nav,
        IAutostartService    autostart)
    {
        _prefs               = prefs;
        _notifications       = notifications;
        _nav                 = nav;
        _autostart           = autostart;
        NotificationsEnabled = _prefs.Get("NotificationsEnabled", "true") == "true";
        // Read the actual registry state (not a cached preference) so the toggle stays
        // correct even if the user disabled autostart via Task Manager > Startup apps.
        AutostartEnabled     = _autostart.IsEnabled();
        DailyLimitText       = _prefs.Get("DailyLimitUsd", "");
        MonthlyLimitText     = _prefs.Get("MonthlyLimitUsd", "");
        SaveConfirmation     = string.Empty;
    }

    [RelayCommand]
    Task GoToPricingAsync() => _nav.GoToAsync("pricing");

    [RelayCommand]
    public async Task TestNotificationAsync()
    {
        _notifications.Show(
            "Test notification",
            "Tray notifications are working correctly.",
            NotificationSeverity.Info);
        SaveConfirmation = "Test notification sent ✓";
        await Task.Delay(2500);
        SaveConfirmation = string.Empty;
    }

    partial void OnNotificationsEnabledChanged(bool value)
        => _prefs.Set("NotificationsEnabled", value ? "true" : "false");

    partial void OnAutostartEnabledChanged(bool value)
        => _autostart.SetEnabled(value);

    partial void OnSaveConfirmationChanged(string value)
        => OnPropertyChanged(nameof(HasSaveConfirmation));

    [RelayCommand]
    public async Task SaveLimitsAsync()
    {
        // Validate: allow empty (= no limit) or a valid positive decimal
        var daily   = DailyLimitText.Trim();
        var monthly = MonthlyLimitText.Trim();

        if (daily != "" && (!decimal.TryParse(daily,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) || d < 0))
        {
            SaveConfirmation = "⚠ Daily limit must be a positive number (e.g. 5.00)";
            return;
        }
        if (monthly != "" && (!decimal.TryParse(monthly,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var m) || m < 0))
        {
            SaveConfirmation = "⚠ Monthly limit must be a positive number (e.g. 20.00)";
            return;
        }

        _prefs.Set("DailyLimitUsd",   daily);
        _prefs.Set("MonthlyLimitUsd", monthly);

        SaveConfirmation = "Saved ✓";
        await Task.Delay(2000);
        SaveConfirmation = string.Empty;
    }
}
