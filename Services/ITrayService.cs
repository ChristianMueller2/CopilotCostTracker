// Services/ITrayService.cs
namespace CopilotCostTracker.Services;

public interface ITrayService
{
    void UpdateTooltip(string text);
    void UpdateTrayIcon(decimal dailyCost, decimal dailyLimit);
}
