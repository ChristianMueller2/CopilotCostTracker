// Services/IFolderWatcherService.cs
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

public interface IFolderWatcherService : IDisposable
{
    void SetFolders(IEnumerable<WatchedFolder> folders);
    event EventHandler<string>? FileChanged;
}
