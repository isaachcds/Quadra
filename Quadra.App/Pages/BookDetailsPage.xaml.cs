using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class BookDetailsPage : ContentPage
{
    public BookDetailsPage(
        BookDetailsViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}