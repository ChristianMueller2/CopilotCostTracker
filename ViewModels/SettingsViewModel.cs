// ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopilotCostTracker.Services;

namespace CopilotCostTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IPreferencesService  _prefs;
    private readonly INotificationService _notifications;

    [ObservableProperty] public partial bool   NotificationsEnabled { get; set; }
    [ObservableProperty] public partial string DailyLimitText       { get; set; }
    [ObservableProperty] public partial string MonthlyLimitText     { get; set; }
    [ObservableProperty] public partial string SaveConfirmation      { get; set; }

    public bool HasSaveConfirmation => !string.IsNullOrEmpty(SaveConfirmation);

    public SettingsViewModel(IPreferencesService prefs, INotificationService notifications)
    {
        _prefs               = prefs;
        _notifications       = notifications;
        NotificationsEnabled = _prefs.Get("NotificationsEnabled", "true") == "true";
        DailyLimitText       = _prefs.Get("DailyLimitUsd", "");
        MonthlyLimitText     = _prefs.Get("MonthlyLimitUsd", "");
        SaveConfirmation     = string.Empty;
    }

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
