using Microsoft.Extensions.DependencyInjection;

namespace Melodix.Presentation;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _serviceProvider;

    // Guarda el proveedor de servicios para crear la ventana principal.
    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    // Crea la ventana inicial de la aplicacion.
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        return new Window(new NavigationPage(mainPage));
    }
}
