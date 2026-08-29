using Quadra.App.Presentation;
using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly ConfiguracoesViewModel _viewModel;

    public SettingsPage(ConfiguracoesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarArmazenamentoAsync();
    }

    private void OnTemaSistema(object? sender, EventArgs e) => _viewModel.TemaSelecionado = TemaAplicativo.Sistema;
    private void OnTemaClaro(object? sender, EventArgs e) => _viewModel.TemaSelecionado = TemaAplicativo.Claro;
    private void OnTemaEscuro(object? sender, EventArgs e) => _viewModel.TemaSelecionado = TemaAplicativo.Escuro;

    private async void OnLimparCacheClicked(object? sender, EventArgs e)
    {
        var confirmou = await DisplayAlertAsync(
            "Limpar cache",
            "Os caches de leitura serão regenerados quando necessário. Obras, capas, progresso e banco serão preservados.",
            "Limpar",
            "Cancelar");

        if (!confirmou)
            return;

        try
        {
            await _viewModel.LimparCacheAsync();
            await DisplayAlertAsync("Cache limpo", "Os números de armazenamento foram atualizados.", "OK");
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            await DisplayAlertAsync("Não foi possível limpar o cache", "Tente novamente em alguns instantes.", "OK");
        }
    }
}
