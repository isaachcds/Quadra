using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _viewModel;

    public LibraryPage(LibraryViewModel viewModel)
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
        _viewModel.CancelImport();
        _viewModel.CancelPendingSearch();
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
