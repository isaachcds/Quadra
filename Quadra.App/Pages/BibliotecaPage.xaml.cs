using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class BibliotecaPage : ContentPage
{
    private readonly BibliotecaViewModel _viewModel;

    public BibliotecaPage(BibliotecaViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel.CarregarBibliotecaCommand.CanExecute(null))
            _viewModel.CarregarBibliotecaCommand.Execute(null);
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _viewModel.CancelarImportacao();
        _viewModel.CancelarBuscaPendente();
        base.OnNavigatedFrom(args);
    }

    private async void OnSearchButtonClicked(object? sender, EventArgs e)
    {
        if (_viewModel.AbrirBuscaCommand.CanExecute(null))
            _viewModel.AbrirBuscaCommand.Execute(null);

        await Task.Delay(50);
        SearchEntry.Focus();
    }
}
