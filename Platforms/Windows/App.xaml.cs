using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CopilotCostTracker.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.UnhandledException += OnUnhandledException;
		this.InitializeComponent();
	}

	private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		var logPath = System.IO.Path.Combine(
			System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
			"CopilotCostTracker_crash.txt");
		System.IO.File.WriteAllText(logPath,
			$"[{DateTime.Now:O}] Unhandled exception:\n{e.Exception}");
		e.Handled = false; // let it crash so we see the log
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

