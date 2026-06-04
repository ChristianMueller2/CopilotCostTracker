// Models/WatchedFolder.cs
namespace CopilotCostTracker.Models;

public class WatchedFolder
{
    public string Path { get; set; } = string.Empty;
    public bool IncludeSubdirectories { get; set; } = true;
}
