using Quadra.App.Presentation;

namespace Quadra.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        return new Window(
            _services.GetRequiredService<AppShell>());
    }
}
