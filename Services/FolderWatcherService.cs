// Services/FolderWatcherService.cs
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

public class FolderWatcherService : IFolderWatcherService
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
    private System.Threading.Timer? _debounceTimer;
    private string _lastChangedFile = string.Empty;
    private readonly object _lock = new();

    public event EventHandler<string>? FileChanged;

    public void SetFolders(IEnumerable<WatchedFolder> folders)
    {
        lock (_lock)
        {
            foreach (var w in _watchers.Values) w.Dispose();
            _watchers.Clear();

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder.Path)) continue;

                var watcher = new FileSystemWatcher(folder.Path, "*.jsonl")
                {
                    IncludeSubdirectories = folder.IncludeSubdirectories,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                watcher.Changed += OnFileEvent;
                watcher.Created += OnFileEvent;
                watcher.Renamed += OnFileEvent;
                _watchers[folder.Path] = watcher;
            }
        }
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        _lastChangedFile = e.FullPath;
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(
            _ => FileChanged?.Invoke(this, _lastChangedFile),
            null, 800, Timeout.Infinite);
    }

    public void Dispose()
    {
        _debounceTimer?.Dispose();
        lock (_lock)
        {
            foreach (var w in _watchers.Values) w.Dispose();
            _watchers.Clear();
        }
    }
}
