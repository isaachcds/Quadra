using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Services;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public partial class ReaderViewModel : ObservableObject, IQueryAttributable
{
    private readonly ComicReaderService _comicReaderService;
    private readonly QuadraDatabase _database;

    public ObservableCollection<ComicPage> Paginas { get; } = [];

    [ObservableProperty]
    private LibraryItem? item;

    [ObservableProperty]
    private int paginaAtual;

    [ObservableProperty]
    private bool estaCarregando;

    [ObservableProperty]
    private bool controlesVisiveis = true;

    [ObservableProperty]
    private string textoPagina = string.Empty;

    public ReaderViewModel(
        ComicReaderService comicReaderService,
        QuadraDatabase database)
    {
        _comicReaderService = comicReaderService;
        _database = database;
    }

    public async void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (!query.TryGetValue("Item", out var value))
            return;

        if (value is not LibraryItem libraryItem)
            return;

        Item = libraryItem;

        await CarregarPaginasAsync();
    }

    private async Task CarregarPaginasAsync()
    {
        if (Item is null)
            return;

        try
        {
            EstaCarregando = true;

            var paginas =
                await _comicReaderService.LoadPagesAsync(Item);

            Paginas.Clear();

            foreach (var pagina in paginas)
                Paginas.Add(pagina);

            PaginaAtual = Math.Clamp(
                Item.CurrentPage,
                0,
                Math.Max(0, Paginas.Count - 1));

            AtualizarTextoPagina();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao abrir leitura",
                ex.Message,
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    partial void OnPaginaAtualChanged(int value)
    {
        AtualizarTextoPagina();

        _ = SalvarProgressoAsync(value);
    }

    private async Task SalvarProgressoAsync(int pagina)
    {
        if (Item is null || Paginas.Count == 0)
            return;

        Item.CurrentPage = pagina;
        Item.TotalPages = Paginas.Count;
        Item.LastReadAt = DateTime.Now;

        await _database.SaveLibraryItemAsync(Item);
    }

    private void AtualizarTextoPagina()
    {
        TextoPagina = Paginas.Count == 0
            ? string.Empty
            : $"{PaginaAtual + 1} / {Paginas.Count}";
    }


    [RelayCommand]
    private void AlternarControles()
    {
        ControlesVisiveis = !ControlesVisiveis;
    }

    [RelayCommand]
    private void AvancarPagina()
    {
        if (Paginas.Count == 0)
            return;

        if (PaginaAtual < Paginas.Count - 1)
            PaginaAtual++;
    }

    [RelayCommand]
    private void VoltarPagina()
    {
        if (Paginas.Count == 0)
            return;

        if (PaginaAtual > 0)
            PaginaAtual--;
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}