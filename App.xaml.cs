// App.xaml.cs
using CopilotCostTracker.Services;

namespace CopilotCostTracker;

public partial class App : Application
{
    private TrayService? _trayService;
    private readonly AppShell _shell;

    public App(TrayService trayService, IServiceProvider serviceProvider)
    {
        InitializeComponent(); // must run first so App.xaml resources are available
        _trayService = trayService;
        _shell = serviceProvider.GetRequiredService<AppShell>(); // resolved after resources are loaded
    }

    protected override Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
    {
        var window = new Microsoft.Maui.Controls.Window(_shell);

        window.Created += (s, e) =>
        {
            var nativeWindow = (MauiWinUIWindow)window.Handler!.PlatformView!;
            var appWindow    = nativeWindow.AppWindow;

            appWindow.Closing += (_, args) =>
            {
                args.Cancel = true;
                appWindow.Hide();
            };
        };

        _trayService?.Initialize(ShowWindow, ExitApp);

        return window;
    }

    private void ShowWindow()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Windows.Count == 0) return;
            var nativeWindow = (MauiWinUIWindow)Windows[0].Handler!.PlatformView!;
            nativeWindow.AppWindow.Show();
            nativeWindow.Activate();
        });
    }

    private void ExitApp()
    {
        _trayService?.Dispose();
        _trayService = null;
        MainThread.BeginInvokeOnMainThread(() => Current?.Quit());
    }
}
