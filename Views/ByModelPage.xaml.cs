// Views/ByModelPage.xaml.cs
using CopilotCostTracker.ViewModels;

namespace CopilotCostTracker.Views;

public partial class ByModelPage : ContentPage
{
    public ByModelPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
