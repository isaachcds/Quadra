using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class ColecoesPage : ContentPage
{
    private readonly ColecoesViewModel _viewModel;

    public ColecoesPage(ColecoesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarAsync();
    }

    private async void OnCriarClicked(object? sender, EventArgs e)
    {
        var nome = await DisplayPromptAsync("Nova coleção", "Nome");
        if (string.IsNullOrWhiteSpace(nome))
            return;

        var descricao = await DisplayPromptAsync("Nova coleção", "Descrição (opcional)");
        await _viewModel.CriarAsync(nome, descricao);
    }

    private async void OnAbrirColecao(object? sender, TappedEventArgs e)
    {
        if ((sender as Grid)?.BindingContext is not CartaoColecao cartao)
            return;

        await Shell.Current.GoToAsync("CollectionDetailsPage", new Dictionary<string, object>
        {
            ["Colecao"] = cartao.Colecao
        });
    }
}
