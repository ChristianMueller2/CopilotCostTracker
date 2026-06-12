// Views/PricingPage.xaml.cs
using CopilotCostTracker.ViewModels;

namespace CopilotCostTracker.Views;

public partial class PricingPage : ContentPage
{
    public PricingPage(PricingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
