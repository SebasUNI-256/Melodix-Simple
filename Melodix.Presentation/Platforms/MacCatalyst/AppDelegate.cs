using Foundation;

namespace Melodix.Presentation;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	// Crea la app MAUI en Mac Catalyst.
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
