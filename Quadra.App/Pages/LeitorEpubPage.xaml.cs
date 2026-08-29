using Quadra.App.ViewModels;
using System.Globalization;

namespace Quadra.App.Pages;

public partial class LeitorEpubPage : ContentPage
{
    private readonly LeitorEpubViewModel _viewModel;
    private bool _estaAtiva;
    private double? _posicaoVerticalPendente;
#if ANDROID
    private Android.Webkit.WebView? _webViewAndroid;
    private InterceptadorToqueWebView? _interceptadorToque;
#endif

    public LeitorEpubPage(LeitorEpubViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        EpubWebView.HandlerChanged += OnWebViewHandlerChanged;
    }

    protected override void OnAppearing()
    {
        _estaAtiva = true;
        base.OnAppearing();
        ConfigurarInterceptadorDeToque();
        _viewModel.AtivarModoFoco();
    }

    protected override async void OnDisappearing()
    {
        _estaAtiva = false;
        _posicaoVerticalPendente = null;
        RemoverInterceptadorDeToque();
        await _viewModel.FecharAsync();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.VoltarCommand.CanExecute(null))
            _viewModel.VoltarCommand.Execute(null);

        return true;
    }

    private async void OnTemaClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string tema })
            await AtualizarAparenciaPreservandoPosicaoAsync(() => _viewModel.DefinirTemaAsync(tema));
    }

    private async void OnFonteClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string fonte })
            await AtualizarAparenciaPreservandoPosicaoAsync(() => _viewModel.DefinirFonteAsync(fonte));
    }

    private async void OnAlinhamentoClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string alinhamento })
            await AtualizarAparenciaPreservandoPosicaoAsync(() => _viewModel.DefinirAlinhamentoAsync(alinhamento));
    }

    private async void OnTamanhoTextoDragCompleted(object? sender, EventArgs e)
    {
        if (sender is Slider slider)
            await AtualizarAparenciaPreservandoPosicaoAsync(() => _viewModel.DefinirTamanhoTextoAsync(slider.Value));
    }

    private async void OnEspacamentoLinhasDragCompleted(object? sender, EventArgs e)
    {
        if (sender is Slider slider)
            await AtualizarAparenciaPreservandoPosicaoAsync(() => _viewModel.DefinirEspacamentoLinhasAsync(slider.Value));
    }

    private async void OnMargemDragCompleted(object? sender, EventArgs e)
    {
        if (sender is Slider slider)
            await AtualizarAparenciaPreservandoPosicaoAsync(() => _viewModel.DefinirMargemLeituraAsync(slider.Value));
    }

    private async Task AtualizarAparenciaPreservandoPosicaoAsync(Func<Task> atualizar)
    {
        if (!_estaAtiva)
            return;

        _posicaoVerticalPendente = await ObterPosicaoVerticalAsync();
        await atualizar();
    }

    private async Task<double?> ObterPosicaoVerticalAsync()
    {
        try
        {
            var resultado = await EpubWebView.EvaluateJavaScriptAsync(
                "String(window.scrollY || document.documentElement.scrollTop || document.body.scrollTop || 0)");

            return double.TryParse(
                resultado?.Trim('"'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var posicao)
                ? Math.Max(0, posicao)
                : null;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            return null;
        }
    }

    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (!_estaAtiva)
            return;

        if (_posicaoVerticalPendente is { } posicao)
        {
            _posicaoVerticalPendente = null;

            try
            {
                var valorJavaScript = posicao.ToString(CultureInfo.InvariantCulture);

                await EpubWebView.EvaluateJavaScriptAsync(
                    $"window.scrollTo(0, {valorJavaScript});");
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
            }
        }

        _viewModel.RegistrarInteracao();
    }

    private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (_viewModel.IsLocalNavigationAllowed(e.Url))
            return;

        e.Cancel = true;
        if (!_estaAtiva ||
            !Uri.TryCreate(e.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Abrir link externo",
            "Deseja abrir este link no navegador?",
            "Abrir",
            "Cancelar");

        if (confirmed && _estaAtiva)
            await Launcher.Default.OpenAsync(uri);
    }

    private void OnWebViewHandlerChanged(object? sender, EventArgs e) => ConfigurarInterceptadorDeToque();

    private void ConfigurarInterceptadorDeToque()
    {
#if ANDROID
        if (!_estaAtiva || EpubWebView.Handler?.PlatformView is not Android.Webkit.WebView webView)
            return;

        if (ReferenceEquals(_webViewAndroid, webView))
            return;

        RemoverInterceptadorDeToque();
        _webViewAndroid = webView;
        _interceptadorToque = new InterceptadorToqueWebView(AlternarControlesPorToque);
        _webViewAndroid.SetOnTouchListener(_interceptadorToque);
#endif
    }

    private void RemoverInterceptadorDeToque()
    {
#if ANDROID
        if (_webViewAndroid is not null)
            _webViewAndroid.SetOnTouchListener(null);

        _interceptadorToque?.Dispose();
        _interceptadorToque = null;
        _webViewAndroid = null;
#endif
    }

    private void AlternarControlesPorToque()
    {
        if (!_estaAtiva || _viewModel.PainelAparenciaVisivel)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_estaAtiva && _viewModel.AlternarControlesCommand.CanExecute(null))
                _viewModel.AlternarControlesCommand.Execute(null);
        });
    }

#if ANDROID
    private sealed class InterceptadorToqueWebView : Java.Lang.Object, Android.Views.View.IOnTouchListener
    {
        private readonly Android.Views.GestureDetector _detector;

        public InterceptadorToqueWebView(Action aoTocar)
        {
            _detector = new Android.Views.GestureDetector(Android.App.Application.Context, new DetectorToqueSimples(aoTocar));
        }

        public bool OnTouch(Android.Views.View? view, Android.Views.MotionEvent? evento)
        {
            if (evento is not null)
                _detector.OnTouchEvent(evento);

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _detector.Dispose();

            base.Dispose(disposing);
        }
    }

    private sealed class DetectorToqueSimples : Android.Views.GestureDetector.SimpleOnGestureListener
    {
        private readonly Action _aoTocar;

        public DetectorToqueSimples(Action aoTocar) => _aoTocar = aoTocar;

        public override bool OnSingleTapUp(Android.Views.MotionEvent? e)
        {
            _aoTocar();
            return true;
        }
    }
#endif
}
