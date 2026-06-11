using Android.App;
using Android.Runtime;

namespace Melodix.Presentation;

[Application]
public class MainApplication : MauiApplication
{
	// Conecta el arranque Android con MAUI.
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	// Crea la app MAUI para Android.
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
