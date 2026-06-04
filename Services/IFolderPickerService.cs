// Services/IFolderPickerService.cs
namespace CopilotCostTracker.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}
