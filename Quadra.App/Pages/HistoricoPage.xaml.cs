using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class HistoricoPage : ContentPage
{
    private readonly HistoricoViewModel _viewModel;

    public HistoricoPage(HistoricoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarAsync();
    }

    private async void OnAbrir(object? sender, TappedEventArgs e)
    {
        if ((sender as Element)?.BindingContext is ItemHistorico item)
            await AbrirDetalhesAsync(item);
    }

    private async void OnContinuar(object? sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is ItemHistorico item)
            await AbrirDetalhesAsync(item);
    }

    private static Task AbrirDetalhesAsync(ItemHistorico item) =>
        Shell.Current.GoToAsync("BookDetailsPage", new Dictionary<string, object>
        {
            ["Item"] = item.Obra
        });
}
