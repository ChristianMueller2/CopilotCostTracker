// Services/ISessionParserService.cs
using CopilotCostTracker.Models;

namespace CopilotCostTracker.Services;

public interface ISessionParserService
{
    Task<IReadOnlyList<CopilotSession>> ParseFileAsync(string filePath, string sourceFolder);
    Task<IReadOnlyList<CopilotSession>> ParseFolderAsync(string folderPath, bool includeSubdirectories);
    Task<IReadOnlyList<CopilotSession>> ParseAllFoldersAsync(IEnumerable<WatchedFolder> folders);
    Task<IReadOnlyList<SessionEvent>> ParseRawEventsAsync(string filePath);
}
