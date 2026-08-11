// Services/DefaultFolderSeeder.cs
using System.Text.Json;
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

/// <summary>
/// Seeds the "WatchedFolders" preference with well-known default paths on first run,
/// so a fresh install starts watching the standard Copilot log locations right away
/// instead of requiring the user to open the Folders page first.
/// </summary>
public static class DefaultFolderSeeder
{
    /// <summary>Default paths to auto-add on first launch (if they exist).</summary>
    public static IEnumerable<string> DefaultPaths()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Path.Combine(profile, ".copilot", "session-state");
        yield return Path.Combine(appData, "Code", "User", "workspaceStorage");
        yield return Path.Combine(profile, ".copilot", "eclipse");
    }

    /// <summary>
    /// If no folders are persisted yet, adds any existing default paths and saves them.
    /// Safe to call multiple times — a no-op once the preference key has been written
    /// (even to an empty list, e.g. if the user intentionally removed all folders), since
    /// we only seed when the key itself has never been set.
    /// Returns true if folders were added.
    /// </summary>
    public static bool SeedIfMissing(IPreferencesService prefs)
    {
        // Distinguish "never set" from "user removed all folders": only seed when the
        // preference key itself hasn't been written yet.
        const string missingSentinel = "__missing__";
        var raw = prefs.Get("WatchedFolders", missingSentinel);
        if (raw != missingSentinel) return false;

        var folders = new List<WatchedFolder>();
        foreach (var path in DefaultPaths())
        {
            if (!Directory.Exists(path)) continue;
            folders.Add(new WatchedFolder { Path = path, IncludeSubdirectories = true });
        }

        var json = JsonSerializer.Serialize(folders.ToArray(), AppJsonContext.Default.WatchedFolderArray);
        prefs.Set("WatchedFolders", json);
        return folders.Count > 0;
    }
}
