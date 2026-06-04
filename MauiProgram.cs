// MauiProgram.cs
using CommunityToolkit.Maui;
using CopilotCostTracker.Services;
using CopilotCostTracker.ViewModels;
using CopilotCostTracker.Views;

namespace CopilotCostTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        // Platform abstractions
        builder.Services.AddSingleton<INavigationService, MauiNavigationService>();
        builder.Services.AddSingleton<IFolderPickerService, MauiFolderPickerService>();
        builder.Services.AddSingleton<IPreferencesService, MauiPreferencesService>();
        builder.Services.AddSingleton<IUserMessageService, MauiUserMessageService>();

        // Domain services
        builder.Services.AddSingleton<PricingService>();
        builder.Services.AddSingleton<ISessionParserService, SessionParserService>();
        builder.Services.AddSingleton<IFolderWatcherService, FolderWatcherService>();
        builder.Services.AddSingleton<TrayService>();

        // ViewModels
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<FolderListViewModel>();
        builder.Services.AddTransient<SessionDetailViewModel>();

        // Shell and pages
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<SessionsPage>();
        builder.Services.AddTransient<ByModelPage>();
        builder.Services.AddTransient<FolderListPage>();
        builder.Services.AddTransient<SessionDetailPage>();

        return builder.Build();
    }
}
