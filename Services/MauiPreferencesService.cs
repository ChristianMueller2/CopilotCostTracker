// Services/MauiPreferencesService.cs
namespace CopilotCostTracker.Services;

public class MauiPreferencesService : IPreferencesService
{
    public string Get(string key, string defaultValue) => Preferences.Default.Get(key, defaultValue);
    public void Set(string key, string value)          => Preferences.Default.Set(key, value);
}
