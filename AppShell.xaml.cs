// AppShell.xaml.cs
using CopilotCostTracker.ViewModels;
using CopilotCostTracker.Views;

namespace CopilotCostTracker;

public partial class AppShell : Shell
{
    public AppShell(MainViewModel mainVm)
    {
        InitializeComponent();
        BindingContext = mainVm;

        // Register detail routes (not in flyout)
        Routing.RegisterRoute("folders",       typeof(FolderListPage));
        Routing.RegisterRoute("sessiondetail", typeof(SessionDetailPage));
    }
}
