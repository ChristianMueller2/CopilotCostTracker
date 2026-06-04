// Views/SessionsPage.xaml.cs
using CopilotCostTracker.Models;
using CopilotCostTracker.ViewModels;

namespace CopilotCostTracker.Views;

public partial class SessionsPage : ContentPage
{
    public SessionsPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnSessionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CopilotSession session)
            return;

        // Clear selection so tapping the same row again works
        SessionList.SelectedItem = null;

        var encodedPath = Uri.EscapeDataString(session.SourceFile);
        await Shell.Current.GoToAsync($"sessiondetail?filePath={encodedPath}");
    }
}
