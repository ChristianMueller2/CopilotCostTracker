// Views/SessionDetailPage.xaml.cs
using CopilotCostTracker.ViewModels;

namespace CopilotCostTracker.Views;

public partial class SessionDetailPage : ContentPage
{
    public SessionDetailPage(SessionDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
