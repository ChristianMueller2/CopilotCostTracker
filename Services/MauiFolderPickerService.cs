// Services/MauiFolderPickerService.cs
using CommunityToolkit.Maui.Storage;

namespace CopilotCostTracker.Services;

public class MauiFolderPickerService : IFolderPickerService
{
    public async Task<string?> PickFolderAsync()
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        return result.IsSuccessful ? result.Folder.Path : null;
    }
}
