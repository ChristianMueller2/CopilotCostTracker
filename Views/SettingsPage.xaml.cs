// Views/SettingsPage.xaml.cs
using CopilotCostTracker.ViewModels;

namespace CopilotCostTracker.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
