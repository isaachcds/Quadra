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
}