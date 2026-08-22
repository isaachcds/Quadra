using Quadra.App.Controls;
using Quadra.App.Services;
using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class ReaderPage : ContentPage
{
    private static readonly TimeSpan SingleTapDelay = TimeSpan.FromMilliseconds(280);
    private static readonly TimeSpan SwipeQuietPeriod = TimeSpan.FromMilliseconds(180);

    private readonly ReaderViewModel _viewModel;
    private CancellationTokenSource? _singleTapCancellation;
    private CancellationTokenSource? _swipeCancellation;
    private bool _isPageZoomed;
    private bool _isSwipeInProgress;
    private DateTime _lastDoubleTapAtUtc = DateTime.MinValue;

    public ReaderPage(ReaderViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ActivateFocusMode();
    }

    protected override async void OnDisappearing()
    {
        CancelGestureWork();
        await _viewModel.CloseAsync();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        NavigateBackSafely();
        return true;
    }

    private async void OnReaderTapped(object? sender, TappedEventArgs e)
    {
        _singleTapCancellation?.Cancel();
        _singleTapCancellation?.Dispose();
        _singleTapCancellation = new CancellationTokenSource();
        var cancellationToken = _singleTapCancellation.Token;

        try
        {
            await Task.Delay(SingleTapDelay, cancellationToken);
            var doubleTapDetected =
                DateTime.UtcNow - _lastDoubleTapAtUtc < TimeSpan.FromMilliseconds(450);
            var position = e.GetPosition(ReaderCarousel);
            if (position is null || ReaderCarousel.Width <= 0)
                return;

            var action = ReaderPresentationLogic.DecideTap(
                position.Value.X / ReaderCarousel.Width,
                _viewModel.NavegacaoPorToqueAtivada,
                _isPageZoomed,
                isPanning: _isPageZoomed,
                _isSwipeInProgress,
                isDoubleTap: doubleTapDetected);

            switch (action)
            {
                case ReaderTapAction.Previous:
                    GoToPreviousPage();
                    break;
                case ReaderTapAction.Next:
                    GoToNextPage();
                    break;
                case ReaderTapAction.ToggleControls:
                    if (_viewModel.AlternarControlesCommand.CanExecute(null))
                        _viewModel.AlternarControlesCommand.Execute(null);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Um toque duplo ou gesto mais recente assumiu a interação.
        }
    }

    private void OnZoomDoubleTapDetected(object? sender, EventArgs e)
    {
        _lastDoubleTapAtUtc = DateTime.UtcNow;
        _singleTapCancellation?.Cancel();
        _viewModel.RegisterInteraction();
    }

    private void OnZoomStateChanged(object? sender, ZoomStateChangedEventArgs e)
    {
        _isPageZoomed = e.IsZoomed;
        ReaderCarousel.IsSwipeEnabled = !e.IsZoomed;
        _viewModel.RegisterInteraction();
    }

    private void OnCarouselScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        _isSwipeInProgress = true;
        _swipeCancellation?.Cancel();
        _swipeCancellation?.Dispose();
        _swipeCancellation = new CancellationTokenSource();
        _ = ClearSwipeStateAsync(_swipeCancellation.Token);
    }

    private void OnCarouselPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        _isPageZoomed = false;
        ReaderCarousel.IsSwipeEnabled = true;

        foreach (var visibleView in ReaderCarousel.VisibleViews)
            FindZoomableImage(visibleView)?.ResetZoom();

        _viewModel.RegisterInteraction();
    }

    private void OnPreviousClicked(object? sender, EventArgs e) => GoToPreviousPage();
    private void OnNextClicked(object? sender, EventArgs e) => GoToNextPage();

    private void OnSliderDragCompleted(object? sender, EventArgs e)
    {
        if (sender is not Slider slider)
            return;

        _viewModel.SetPageFromSlider(slider.Value);
        ReaderCarousel.ScrollTo(
            _viewModel.PaginaAtual,
            position: ScrollToPosition.Center,
            animate: false);
    }

    private void GoToPreviousPage()
    {
        if (!_viewModel.VoltarPaginaCommand.CanExecute(null))
            return;

        _viewModel.VoltarPaginaCommand.Execute(null);
        ScrollToCurrentPage();
    }

    private void GoToNextPage()
    {
        if (!_viewModel.AvancarPaginaCommand.CanExecute(null))
            return;

        _viewModel.AvancarPaginaCommand.Execute(null);
        ScrollToCurrentPage();
    }

    private void ScrollToCurrentPage()
    {
        ReaderCarousel.ScrollTo(
            _viewModel.PaginaAtual,
            position: ScrollToPosition.Center,
            animate: true);
    }

    private async Task ClearSwipeStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SwipeQuietPeriod, cancellationToken);
            _isSwipeInProgress = false;
        }
        catch (OperationCanceledException)
        {
            // O Carousel ainda está se movimentando.
        }
    }

    private void NavigateBackSafely()
    {
        if (_viewModel.VoltarCommand.CanExecute(null))
            _viewModel.VoltarCommand.Execute(null);
    }

    private void CancelGestureWork()
    {
        _singleTapCancellation?.Cancel();
        _swipeCancellation?.Cancel();
    }

    private static ZoomableImage? FindZoomableImage(IVisualTreeElement element)
    {
        if (element is ZoomableImage zoomableImage)
            return zoomableImage;

        foreach (var child in element.GetVisualChildren())
        {
            var result = FindZoomableImage(child);
            if (result is not null)
                return result;
        }

        return null;
    }
}
