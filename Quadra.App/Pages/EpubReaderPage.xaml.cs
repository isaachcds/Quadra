using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class EpubReaderPage : ContentPage
{
    public EpubReaderPage(
        EpubReaderViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}