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
    private readonly SemaphoreSlim _progressLock = new(1, 1);
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _progressCancellation;
    private int _progressVersion;

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
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();

        await CarregarPaginasAsync(_loadCancellation.Token);
    }

    private async Task CarregarPaginasAsync(CancellationToken cancellationToken)
    {
        if (Item is null)
            return;

        try
        {
            EstaCarregando = true;

            var paginas =
                await _comicReaderService.LoadPagesAsync(Item, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            Paginas.Clear();

            foreach (var pagina in paginas)
                Paginas.Add(pagina);

            PaginaAtual = Math.Clamp(
                Item.CurrentPage,
                0,
                Math.Max(0, Paginas.Count - 1));

            AtualizarTextoPagina();
        }
        catch (OperationCanceledException)
        {
            // A página deixou de precisar desta preparação.
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

        EnfileirarSalvamento(value);
    }

    private void EnfileirarSalvamento(int pagina)
    {
        _progressCancellation?.Cancel();
        _progressCancellation?.Dispose();
        _progressCancellation = new CancellationTokenSource();

        var version = Interlocked.Increment(ref _progressVersion);
        _ = SalvarProgressoSeguroAsync(
            pagina,
            version,
            debounce: true,
            _progressCancellation.Token);
    }

    private async Task SalvarProgressoSeguroAsync(
        int pagina,
        int version,
        bool debounce,
        CancellationToken cancellationToken)
    {
        try
        {
            if (debounce)
                await Task.Delay(250, cancellationToken);

            await _progressLock.WaitAsync(cancellationToken);

            try
            {
                if (version != Volatile.Read(ref _progressVersion) ||
                    Item is null ||
                    Paginas.Count == 0)
                {
                    return;
                }

                Item.CurrentPage = pagina;
                Item.TotalPages = Paginas.Count;
                Item.LastReadAt = DateTime.Now;

                await _database.SaveLibraryItemAsync(Item);
            }
            finally
            {
                _progressLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Uma posição mais nova substituiu esta.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Não foi possível salvar o progresso: {ex}");
        }
    }

    public async Task FlushProgressAsync()
    {
        _progressCancellation?.Cancel();
        var version = Interlocked.Increment(ref _progressVersion);

        await SalvarProgressoSeguroAsync(
            PaginaAtual,
            version,
            debounce: false,
            CancellationToken.None);
    }

    public void CancelLoading()
    {
        _loadCancellation?.Cancel();
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
