using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class LeitorEpubPage : ContentPage
{
    private readonly LeitorEpubViewModel _viewModel;

    public LeitorEpubPage(
        LeitorEpubViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _viewModel.CancelOperations();
        base.OnNavigatedFrom(args);
    }

    private async void OnWebViewNavigating(
        object? sender,
        WebNavigatingEventArgs e)
    {
        if (_viewModel.IsLocalNavigationAllowed(e.Url))
            return;

        e.Cancel = true;

        if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Abrir link externo",
            "Deseja abrir este link no navegador?",
            "Abrir",
            "Cancelar");

        if (confirmed)
            await Launcher.Default.OpenAsync(uri);
    }
}
