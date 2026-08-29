using Quadra.App;

namespace Quadra.App.Pages;

public partial class AberturaPage : ContentPage
{
    private const uint DuracaoAnimacaoMs = 650;
    private readonly AppShell _appShell;
    private bool _animacaoIniciada;

    public AberturaPage(AppShell appShell)
    {
        InitializeComponent();
        _appShell = appShell;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_animacaoIniciada)
        {
            return;
        }

        _animacaoIniciada = true;

        await Task.WhenAll(
            OpeningLogo.FadeToAsync(1, DuracaoAnimacaoMs, Easing.CubicOut),
            OpeningLogo.ScaleToAsync(1, DuracaoAnimacaoMs, Easing.CubicOut));

        if (Window is not null)
        {
            Window.Page = _appShell;
        }
    }
}
