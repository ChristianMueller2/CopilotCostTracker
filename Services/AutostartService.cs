// Services/AutostartService.cs
using Microsoft.Win32;

namespace CopilotCostTracker.Services;

/// <summary>
/// Registers/unregisters the app to launch at Windows sign-in via the current user's
/// "Run" registry key (HKCU\Software\Microsoft\Windows\CurrentVersion\Run). This works
/// both for the unpackaged (WindowsPackageType=None) build and, unlike the MSIX
/// StartupTask API, requires no manifest extension or first-run user approval dialog.
/// </summary>
public class AutostartService : IAutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName  = "CopilotCostTracker";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath
                              ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;

                // Quote the path in case it contains spaces; pass --minimized so the
                // autostart launch doesn't pop the main window in front of the user.
                key.SetValue(ValueName, $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch { /* best-effort — lack of registry access shouldn't crash the app */ }
    }
}
