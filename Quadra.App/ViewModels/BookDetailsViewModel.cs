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
            return;
        }

        PossuiPaginas = true;

        PercentualProgresso =
            Math.Clamp(
                (double)Item.CurrentPage / Item.TotalPages,
                0,
                1);

        TextoProgresso =
            $"Página {Item.CurrentPage + 1} de {Item.TotalPages}";
    }
}