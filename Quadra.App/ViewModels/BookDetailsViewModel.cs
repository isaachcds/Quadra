using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Pages;
using Quadra.App.Models;
using Quadra.App.Services;

namespace Quadra.App.ViewModels;

public partial class BookDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly QuadraDatabase _database;
    private readonly LibraryStorageService _storageService;
    private readonly ComicReaderService _comicReaderService;

    [ObservableProperty]
    private LibraryItem? item;

    [ObservableProperty]
    private string textoProgresso = "Ainda não iniciado";

    [ObservableProperty]
    private double percentualProgresso;

    [ObservableProperty]
    private bool possuiPaginas;

    [ObservableProperty]
    private bool estaPreparandoLeitura;

    [ObservableProperty]
    private string textoPreparacao = string.Empty;

    [ObservableProperty]
    private string textoBotaoLeitura = "Começar leitura";

    public BookDetailsViewModel(
    QuadraDatabase database,
    LibraryStorageService storageService,
    ComicReaderService comicReaderService)
    {
        _database = database;
        _storageService = storageService;
        _comicReaderService = comicReaderService;
    }

    public void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (!query.TryGetValue("Item", out var value))
            return;

        if (value is not LibraryItem libraryItem)
            return;

        Item = libraryItem;

        AtualizarProgresso();
    }

    [RelayCommand]
    private async Task IniciarLeituraAsync()
    {
        if (Item is null || EstaPreparandoLeitura)
            return;

        var leituraConcluida =
            Item.TotalPages > 0 &&
            Item.CurrentPage >= Item.TotalPages - 1;

        if (leituraConcluida)
        {
            Item.CurrentPage = 0;
        }

        if (Item.Format.Equals(
        "EPUB",
        StringComparison.OrdinalIgnoreCase))
        {
            await Shell.Current.GoToAsync(
                nameof(EpubReaderPage),
                new Dictionary<string, object>
                {
                    ["Item"] = Item
                });

            return;
        }

        try
        {
            EstaPreparandoLeitura = true;
            TextoPreparacao = "Preparando páginas...";

            var paginas =
                await _comicReaderService.LoadPagesAsync(Item);

            if (paginas.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Nenhuma página encontrada",
                    "O arquivo não possui imagens compatíveis.",
                    "OK");

                return;
            }

            Item.TotalPages = paginas.Count;

            if (Item.CurrentPage < 0 ||
                Item.CurrentPage >= Item.TotalPages)
            {
                Item.CurrentPage = 0;
            }

            await _database.SaveLibraryItemAsync(Item);

            AtualizarProgresso();

            var parametros = new Dictionary<string, object>
            {
                ["Item"] = Item
            };

            await Shell.Current.GoToAsync(
                nameof(ReaderPage),
                parametros);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao preparar leitura",
                ex.Message,
                "OK");
        }
        finally
        {
            EstaPreparandoLeitura = false;
            TextoPreparacao = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (Item is null)
            return;

        var confirmou = await Shell.Current.DisplayAlertAsync(
            "Remover obra",
            $"Deseja remover \"{Item.Title}\" da biblioteca?",
            "Remover",
            "Cancelar");

        if (!confirmou)
            return;

        try
        {
            await _database.DeleteLibraryItemAsync(Item);
            await _storageService.DeleteAsync(Item);

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao remover",
                ex.Message,
                "OK");
        }
    }

    private void AtualizarProgresso()
    {
        if (Item is null || Item.TotalPages <= 0)
        {
            PossuiPaginas = false;
            PercentualProgresso = 0;
            TextoProgresso = "Ainda não iniciado";
            TextoBotaoLeitura = "Começar leitura";
            return;
        }

        PossuiPaginas = true;

        var paginaExibida = Math.Clamp(
            Item.CurrentPage + 1,
            1,
            Item.TotalPages);

        var leituraConcluida =
            Item.CurrentPage >= Item.TotalPages - 1;

        PercentualProgresso = Math.Clamp(
            (double)paginaExibida / Item.TotalPages,
            0,
            1);

        if (leituraConcluida)
        {
            TextoProgresso = "Leitura concluída";
            TextoBotaoLeitura = "Ler novamente";
            return;
        }

        TextoProgresso =
            $"Página {paginaExibida} de {Item.TotalPages}";

        TextoBotaoLeitura =
            Item.CurrentPage > 0
                ? "Continuar leitura"
                : "Começar leitura";
    }

    [RelayCommand]
    private async Task AtualizarDetalhesAsync()
    {
        if (Item is null || Item.Id <= 0)
            return;

        try
        {
            var itemAtualizado =
                await _database.GetLibraryItemAsync(Item.Id);

            if (itemAtualizado is null)
                return;

            Item = itemAtualizado;

            AtualizarProgresso();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao atualizar detalhes",
                ex.Message,
                "OK");
        }
    }
}