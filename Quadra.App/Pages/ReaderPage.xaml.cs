using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class ReaderPage : ContentPage
{
    private const string TapNavigationPreferenceKey =
        "ReaderTapNavigationEnabled";

    private readonly ReaderViewModel _viewModel;

    private bool _tapNavigationEnabled;

    public ReaderPage(ReaderViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;

        _tapNavigationEnabled = Preferences.Default.Get(
            TapNavigationPreferenceKey,
            true);
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

        /*
         * No modo "Apenas deslizar", os toques laterais
         * não mudam a página.
         *
         * O toque no centro continua mostrando ou
         * escondendo os controles.
         */
        if (!_tapNavigationEnabled)
        {
            if (touchX > leftLimit &&
                touchX < rightLimit)
            {
                AlternarControles();
            }

            return;
        }

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

        AlternarControles();
    }

    private async void OnReaderSettingsClicked(
        object? sender,
        EventArgs e)
    {
        var modoAtual = _tapNavigationEnabled
            ? "Deslizar e tocar nas laterais"
            : "Apenas deslizar";

        var escolha =
            await DisplayActionSheetAsync(
                $"Navegação atual: {modoAtual}",
                "Cancelar",
                null,
                "Deslizar e tocar nas laterais",
                "Apenas deslizar");

        switch (escolha)
        {
            case "Deslizar e tocar nas laterais":
                SalvarModoNavegacao(
                    tapNavigationEnabled: true);

                await DisplayAlertAsync(
                    "Modo de navegação",
                    "Agora você pode deslizar ou tocar nas laterais para mudar de página.",
                    "OK");
                break;

            case "Apenas deslizar":
                SalvarModoNavegacao(
                    tapNavigationEnabled: false);

                await DisplayAlertAsync(
                    "Modo de navegação",
                    "Agora as páginas serão alteradas somente ao deslizar.",
                    "OK");
                break;
        }
    }

    private void SalvarModoNavegacao(
        bool tapNavigationEnabled)
    {
        _tapNavigationEnabled =
            tapNavigationEnabled;

        Preferences.Default.Set(
            TapNavigationPreferenceKey,
            tapNavigationEnabled);
    }

    private void AlternarControles()
    {
        if (_viewModel
            .AlternarControlesCommand
            .CanExecute(null))
        {
            _viewModel
                .AlternarControlesCommand
                .Execute(null);
        }
    }

    private void VoltarPagina()
    {
        if (!_viewModel
            .VoltarPaginaCommand
            .CanExecute(null))
        {
            return;
        }

        _viewModel
            .VoltarPaginaCommand
            .Execute(null);

        ReaderCarousel.ScrollTo(
            _viewModel.PaginaAtual,
            position: ScrollToPosition.Center,
            animate: true);
    }

    private void AvancarPagina()
    {
        if (!_viewModel
            .AvancarPaginaCommand
            .CanExecute(null))
        {
            return;
        }

        _viewModel
            .AvancarPaginaCommand
            .Execute(null);

        ReaderCarousel.ScrollTo(
            _viewModel.PaginaAtual,
            position: ScrollToPosition.Center,
            animate: true);
    }
}