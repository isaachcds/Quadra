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
}
