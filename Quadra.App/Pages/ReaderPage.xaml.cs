using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class ReaderPage : ContentPage
{
    public ReaderPage(ReaderViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}