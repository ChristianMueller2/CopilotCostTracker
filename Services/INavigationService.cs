// Services/INavigationService.cs
namespace CopilotCostTracker.Services;

public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoBackAsync();
    Task GoToFoldersAsync();
    Task GoToSessionsAsync();
    Task GoToByModelAsync();
    Task GoToOverviewAsync();
}
