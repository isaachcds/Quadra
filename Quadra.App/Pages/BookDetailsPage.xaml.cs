using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class BookDetailsPage : ContentPage
{
    private readonly BookDetailsViewModel _viewModel;

    public BookDetailsPage(
        BookDetailsViewModel viewModel)
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
        _viewModel.CancelPreparation();
        base.OnNavigatedFrom(args);
    }

    private async void OnBackClicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
