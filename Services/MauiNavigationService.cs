// Services/MauiNavigationService.cs
namespace CopilotCostTracker.Services;

public class MauiNavigationService : INavigationService
{
    public Task GoToAsync(string route)    => Shell.Current.GoToAsync(route);
    public Task GoBackAsync()              => Shell.Current.GoToAsync("..");
    public Task GoToFoldersAsync()         => Shell.Current.GoToAsync("folders");
    public Task GoToSessionsAsync()        => Shell.Current.GoToAsync("//sessions");
    public Task GoToByModelAsync()         => Shell.Current.GoToAsync("//bymodel");
    public Task GoToOverviewAsync()        => Shell.Current.GoToAsync("//main");
}
