using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Services;

namespace Quadra.App.ViewModels;

public partial class BookDetailsViewModel : ObservableObject, IQueryAttributable
{
    private readonly QuadraDatabase _database;
    private readonly LibraryStorageService _storageService;

    [ObservableProperty]
    private LibraryItem? item;

    [ObservableProperty]
    private string textoProgresso = "Ainda não iniciado";

    [ObservableProperty]
    private double percentualProgresso;

    [ObservableProperty]
    private bool possuiPaginas;

    public BookDetailsViewModel(
        QuadraDatabase database,
        LibraryStorageService storageService)
    {
        _database = database;
        _storageService = storageService;
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
        if (Item is null)
            return;

        await Shell.Current.DisplayAlertAsync(
            "Leitor",
            $"Na próxima etapa abriremos as páginas de \"{Item.Title}\".",
            "OK");
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
            $"Página {Item.CurrentPage} de {Item.TotalPages}";
    }
}