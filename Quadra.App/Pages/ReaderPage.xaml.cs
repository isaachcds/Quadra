using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class ReaderPage : ContentPage
{
    private readonly ReaderViewModel _viewModel;

    public ReaderPage(ReaderViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnReaderTapped(
        object? sender,
        TappedEventArgs e)
    {
        var position = e.GetPosition(ReaderCarousel);

        if (position is null || ReaderCarousel.Width <= 0)
            return;

        var touchX = position.Value.X;
        var width = ReaderCarousel.Width;

        var leftLimit = width * 0.30;
        var rightLimit = width * 0.70;

        if (touchX <= leftLimit)
        {
            VoltarPagina();
            return;
        }

        if (touchX >= rightLimit)
        {
            AvancarPagina();
            return;
        }

        if (_viewModel.AlternarControlesCommand.CanExecute(null))
        {
            _viewModel.AlternarControlesCommand.Execute(null);
        }
    }

    private void VoltarPagina()
    {
        if (!_viewModel.VoltarPaginaCommand.CanExecute(null))
            return;

        _viewModel.VoltarPaginaCommand.Execute(null);

        ReaderCarousel.ScrollTo(
            _viewModel.PaginaAtual,
            position: ScrollToPosition.Center,
            animate: true);
    }

    private void AvancarPagina()
    {
        if (!_viewModel.AvancarPaginaCommand.CanExecute(null))
            return;

        _viewModel.AvancarPaginaCommand.Execute(null);

        ReaderCarousel.ScrollTo(
            _viewModel.PaginaAtual,
            position: ScrollToPosition.Center,
            animate: true);
    }
}