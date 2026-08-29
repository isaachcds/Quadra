using Quadra.App.Presentation;

namespace Quadra.App;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        PreferenciasAplicativo.AplicarTema(this, PreferenciasAplicativo.ObterTema(Preferences.Default));
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(_services.GetRequiredService<Pages.AberturaPage>());
}
