// Services/IAutostartService.cs
namespace CopilotCostTracker.Services;

/// <summary>Manages whether the app launches automatically when Windows starts.</summary>
public interface IAutostartService
{
    /// <summary>Reads the current state directly from the registry (source of truth,
    /// so it stays correct even if the user disabled it via Task Manager > Startup apps).</summary>
    bool IsEnabled();

    /// <summary>Enables or disables autostart by adding/removing the current executable
    /// from the current user's Run registry key.</summary>
    void SetEnabled(bool enabled);
}
