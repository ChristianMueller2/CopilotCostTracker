// Services/MauiUserMessageService.cs
namespace CopilotCostTracker.Services;

public class MauiUserMessageService : IUserMessageService
{
    public Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page is null
            ? Task.CompletedTask
            : page.DisplayAlertAsync(title, message, cancel);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return false;
        return await page.DisplayAlertAsync(title, message, accept, cancel);
    }
}
