using Quadra.App.Models;
using Quadra.App.ViewModels;

namespace Quadra.App.Pages;

public partial class DetalhesColecaoPage : ContentPage
{
    private readonly DetalhesColecaoViewModel _viewModel;

    public DetalhesColecaoPage(DetalhesColecaoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnVoltar(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnRemover(object? sender, EventArgs e)
    {
        if ((sender as ImageButton)?.CommandParameter is not ObraBiblioteca obra)
            return;

        if (await DisplayAlertAsync("Remover obra", "Remover esta obra da coleção?", "Remover", "Cancelar"))
            await _viewModel.RemoverObraAsync(obra);
    }

    private async void OnAdicionar(object? sender, EventArgs e)
    {
        var opcoes = _viewModel.ObrasDisponiveis
            .Select((obra, indice) => new
            {
                Obra = obra,
                Rotulo = $"{obra.Title} · {obra.Format.ToUpperInvariant()} · {indice + 1}"
            })
            .ToList();

        if (opcoes.Count == 0)
        {
            await DisplayAlertAsync("Adicionar obra", "Todas as obras da biblioteca já pertencem a esta coleção.", "OK");
            return;
        }

        var escolha = await DisplayActionSheetAsync("Adicionar obra", "Cancelar", null, opcoes.Select(opcao => opcao.Rotulo).ToArray());
        var obraSelecionada = opcoes.FirstOrDefault(opcao => opcao.Rotulo == escolha)?.Obra;
        if (obraSelecionada is not null)
            await _viewModel.AdicionarObraAsync(obraSelecionada);
    }

    private async void OnEditar(object? sender, EventArgs e)
    {
        if (_viewModel.Colecao is null)
            return;

        var nome = await DisplayPromptAsync("Editar coleção", "Nome", initialValue: _viewModel.Colecao.Nome);
        if (string.IsNullOrWhiteSpace(nome))
            return;

        var descricao = await DisplayPromptAsync("Editar coleção", "Descrição (opcional)", initialValue: _viewModel.Colecao.Descricao);
        if (descricao is null)
            return;

        await _viewModel.SalvarAsync(nome, descricao);
    }

    private async void OnExcluir(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("Excluir coleção", "As obras e seus arquivos serão preservados na biblioteca.", "Excluir", "Cancelar"))
            return;

        await _viewModel.ExcluirAsync();
        await Shell.Current.GoToAsync("..");
    }

    private async void OnAbrirObra(object? sender, TappedEventArgs e)
    {
        if ((sender as Element)?.BindingContext is not ObraBiblioteca obra)
            return;

        await Shell.Current.GoToAsync("BookDetailsPage", new Dictionary<string, object>
        {
            ["Item"] = obra
        });
    }
}
