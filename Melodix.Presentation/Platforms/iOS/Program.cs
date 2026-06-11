using ObjCRuntime;
using UIKit;

namespace Melodix.Presentation;

public class Program
{
	// Punto de entrada de iOS.
	static void Main(string[] args)
	{
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
