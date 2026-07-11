using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Services;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public partial class EpubReaderViewModel
    : ObservableObject, IQueryAttributable
{
    private readonly IEpubReaderService _epubReaderService;
    private readonly QuadraDatabase _database;

    public ObservableCollection<EpubChapter> Capitulos { get; } = [];

    [ObservableProperty]
    private LibraryItem? item;

    [ObservableProperty]
    private int capituloAtual;

    [ObservableProperty]
    private bool estaCarregando;

    [ObservableProperty]
    private bool controlesVisiveis = true;

    [ObservableProperty]
    private string textoCapitulo = string.Empty;

    [ObservableProperty]
    private string tituloCapitulo = string.Empty;

    [ObservableProperty]
    private WebViewSource? conteudoCapitulo;

    public EpubReaderViewModel(
        IEpubReaderService epubReaderService,
        QuadraDatabase database)
    {
        _epubReaderService = epubReaderService;
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

        await CarregarLivroAsync();
    }

    private async Task CarregarLivroAsync()
    {
        if (Item is null)
            return;

        try
        {
            EstaCarregando = true;

            var capitulos =
                await _epubReaderService.LoadChaptersAsync(Item);

            Capitulos.Clear();

            foreach (var capitulo in capitulos)
                Capitulos.Add(capitulo);

            CapituloAtual = Math.Clamp(
                Item.CurrentPage,
                0,
                Math.Max(0, Capitulos.Count - 1));

            await CarregarCapituloAtualAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Erro ao abrir EPUB",
                ex.Message,
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            EstaCarregando = false;
        }
    }

    private async Task CarregarCapituloAtualAsync()
    {
        if (Capitulos.Count == 0)
            return;

        var indice = Math.Clamp(
            CapituloAtual,
            0,
            Capitulos.Count - 1);

        var capitulo = Capitulos[indice];

        if (!File.Exists(capitulo.LocalFilePath))
        {
            throw new FileNotFoundException(
                "O arquivo do capítulo não foi encontrado.",
                capitulo.LocalFilePath);
        }

        var html = await File.ReadAllTextAsync(
            capitulo.LocalFilePath);

        var directory =
            Path.GetDirectoryName(capitulo.LocalFilePath);

        ConteudoCapitulo = new HtmlWebViewSource
        {
            Html = html,
            BaseUrl = directory
        };

        TituloCapitulo = capitulo.Title;

        AtualizarTextoCapitulo();

        await SalvarProgressoAsync();
    }

    private void AtualizarTextoCapitulo()
    {
        TextoCapitulo = Capitulos.Count == 0
            ? string.Empty
            : $"{CapituloAtual + 1} / {Capitulos.Count}";
    }

    private async Task SalvarProgressoAsync()
    {
        if (Item is null || Capitulos.Count == 0)
            return;

        Item.CurrentPage = CapituloAtual;
        Item.TotalPages = Capitulos.Count;
        Item.LastReadAt = DateTime.Now;

        await _database.SaveLibraryItemAsync(Item);
    }

    [RelayCommand]
    private async Task AvancarCapituloAsync()
    {
        if (Capitulos.Count == 0)
            return;

        if (CapituloAtual >= Capitulos.Count - 1)
            return;

        CapituloAtual++;

        await CarregarCapituloAtualAsync();
    }

    [RelayCommand]
    private async Task VoltarCapituloAsync()
    {
        if (Capitulos.Count == 0)
            return;

        if (CapituloAtual <= 0)
            return;

        CapituloAtual--;

        await CarregarCapituloAtualAsync();
    }

    [RelayCommand]
    private void AlternarControles()
    {
        ControlesVisiveis = !ControlesVisiveis;
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}