using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class DetalhesObraPage : ContentPage
{
    private readonly DetalhesObraViewModel _viewModel;

    public DetalhesObraPage(
        DetalhesObraViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.AtualizarDetalhesCommand.CanExecute(null))
        {
            _viewModel.AtualizarDetalhesCommand.Execute(null);
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _viewModel.CancelarPreparacao();
        base.OnNavigatedFrom(args);
    }

    private async void OnBackClicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnColecaoToggled(object? sender, ToggledEventArgs e)
    {
        if ((sender as Switch)?.BindingContext is OpcaoColecaoObra opcao)
            await _viewModel.AlternarColecaoAsync(opcao, e.Value);
    }

    private async void OnCriarColecaoClicked(object? sender, EventArgs e)
    {
        var nome = await DisplayPromptAsync("Nova coleção", "Nome");
        if (!string.IsNullOrWhiteSpace(nome))
            await _viewModel.CriarColecaoAsync(nome);
    }
}
