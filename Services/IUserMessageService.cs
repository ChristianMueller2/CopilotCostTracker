// Services/IUserMessageService.cs
namespace CopilotCostTracker.Services;

public interface IUserMessageService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
}
