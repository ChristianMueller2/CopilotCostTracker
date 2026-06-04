// Services/IPreferencesService.cs
namespace CopilotCostTracker.Services;

public interface IPreferencesService
{
    string Get(string key, string defaultValue);
    void Set(string key, string value);
}
