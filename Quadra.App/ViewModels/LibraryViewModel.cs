using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Pages;
using Quadra.App.Services;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly LibraryStorageService _storageService;
    private readonly QuadraDatabase _database;
    private readonly CoverService _coverService;
    private readonly LibraryCleanupService _cleanupService;
    private CancellationTokenSource? _importCancellation;

    public ObservableCollection<LibraryItem> Itens { get; } = [];
    public ObservableCollection<LibraryBookViewData> ItensFiltrados { get; } = [];

    public LibraryViewModel(
        LibraryStorageService storageService,
        QuadraDatabase database,
        CoverService coverService,
        LibraryCleanupService cleanupService)
    {
        _storageService = storageService;
        _database = database;
        _coverService = coverService;
        _cleanupService = cleanupService;
    }

    [ObservableProperty]
    private string? nomeArquivoSelecionado;

    [ObservableProperty]
    private bool possuiArquivoSelecionado;

    [ObservableProperty]
    private bool estaImportando;

    [ObservableProperty]
    private LibraryItem? itemImportado;

    [ObservableProperty]
    private bool bibliotecaVazia;

    [ObservableProperty]
    private bool possuiItens;

    private bool _estaCarregandoBiblioteca;
    private bool _bibliotecaPreenchida;
    private bool _filtroSemResultados;
    private bool _temErro;
    private string _mensagemErro = string.Empty;
    private bool _filtrosVisiveis = true;
    private LibraryFormatFilter _filtroSelecionado = LibraryFormatFilter.All;
    private LibraryBookViewData? _continuarLendo;

    public bool EstaCarregandoBiblioteca
    {
        get => _estaCarregandoBiblioteca;
        set => SetProperty(ref _estaCarregandoBiblioteca, value);
    }

    public bool BibliotecaPreenchida
    {
        get => _bibliotecaPreenchida;
        set => SetProperty(ref _bibliotecaPreenchida, value);
    }

    public bool FiltroSemResultados
    {
        get => _filtroSemResultados;
        set => SetProperty(ref _filtroSemResultados, value);
    }

    public bool TemErro
    {
        get => _temErro;
        set => SetProperty(ref _temErro, value);
    }

    public string MensagemErro
    {
        get => _mensagemErro;
        set => SetProperty(ref _mensagemErro, value);
    }

    public bool FiltrosVisiveis
    {
        get => _filtrosVisiveis;
        set => SetProperty(ref _filtrosVisiveis, value);
    }

    public LibraryFormatFilter FiltroSelecionado
    {
        get => _filtroSelecionado;
        set
        {
            if (!SetProperty(ref _filtroSelecionado, value))
                return;

            OnPropertyChanged(nameof(FiltroTodosSelecionado));
            OnPropertyChanged(nameof(FiltroEpubSelecionado));
            OnPropertyChanged(nameof(FiltroPdfSelecionado));
            OnPropertyChanged(nameof(FiltroComicsSelecionado));
            OnPropertyChanged(nameof(DescricaoFiltroTodos));
            OnPropertyChanged(nameof(DescricaoFiltroEpub));
            OnPropertyChanged(nameof(DescricaoFiltroPdf));
            OnPropertyChanged(nameof(DescricaoFiltroComics));
            AtualizarApresentacao();
        }
    }

    public LibraryBookViewData? ContinuarLendo
    {
        get => _continuarLendo;
        set
        {
            if (SetProperty(ref _continuarLendo, value))
                OnPropertyChanged(nameof(PossuiContinuarLendo));
        }
    }

    public bool PodeImportar => !EstaImportando;
    public bool PossuiContinuarLendo => ContinuarLendo is not null;
    public bool FiltroTodosSelecionado => FiltroSelecionado == LibraryFormatFilter.All;
    public bool FiltroEpubSelecionado => FiltroSelecionado == LibraryFormatFilter.Epub;
    public bool FiltroPdfSelecionado => FiltroSelecionado == LibraryFormatFilter.Pdf;
    public bool FiltroComicsSelecionado => FiltroSelecionado == LibraryFormatFilter.Comics;
    public string DescricaoFiltroTodos => FilterDescription("Todos", FiltroTodosSelecionado);
    public string DescricaoFiltroEpub => FilterDescription("EPUB", FiltroEpubSelecionado);
    public string DescricaoFiltroPdf => FilterDescription("PDF", FiltroPdfSelecionado);
    public string DescricaoFiltroComics => FilterDescription("CBR e CBZ", FiltroComicsSelecionado);

    partial void OnEstaImportandoChanged(bool value)
    {
        OnPropertyChanged(nameof(PodeImportar));
    }

    [RelayCommand]
    private async Task ImportarArquivoAsync()
    {
        if (EstaImportando)
            return;

        try
        {
            EstaImportando = true;
            _importCancellation?.Dispose();
            _importCancellation = new CancellationTokenSource();
            var cancellationToken = _importCancellation.Token;

            var tiposPermitidos = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = ["*/*"],
                    [DevicePlatform.WinUI] = [".pdf", ".cbz", ".cbr", ".epub"]
                });

            var arquivo = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione um CBR, CBZ, PDF ou EPUB",
                FileTypes = tiposPermitidos
            });

            if (arquivo is null)
                return;

            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

            if (!SupportedFileFormats.IsSupported(extensao))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Arquivo não suportado",
                    "Escolha um arquivo CBR, CBZ, PDF ou EPUB.",
                    "Entendi");
                return;
            }

            ItemImportado = await _storageService.ImportAsync(
                arquivo,
                cancellationToken);

            ItemImportado.CoverPath = await _coverService.GenerateCoverAsync(
                ItemImportado,
                cancellationToken);

            await _database.SaveLibraryItemAsync(ItemImportado);
            Itens.Insert(0, ItemImportado);
            AtualizarApresentacao();

            NomeArquivoSelecionado = ItemImportado.OriginalFileName;
            PossuiArquivoSelecionado = true;

            await Shell.Current.DisplayAlertAsync(
                "Importação concluída",
                $"{ItemImportado.Title} foi copiado para a biblioteca do Quadra.",
                "OK");
        }
        catch (OperationCanceledException)
        {
            await CleanupIncompleteImportAsync();
        }
        catch (Exception ex)
        {
            await CleanupIncompleteImportAsync();
            await Shell.Current.DisplayAlertAsync("Erro ao importar", ex.Message, "OK");
        }
        finally
        {
            EstaImportando = false;
        }
    }

    public void CancelImport()
    {
        _importCancellation?.Cancel();
    }

    private async Task CleanupIncompleteImportAsync()
    {
        if (ItemImportado is null || ItemImportado.Id > 0)
            return;

        await _storageService.DeleteAsync(ItemImportado);
        ItemImportado = null;
    }

    [RelayCommand]
    private async Task ExcluirItemAsync(LibraryItem? item)
    {
        if (item is null)
            return;

        var confirmou = await Shell.Current.DisplayAlertAsync(
            "Remover obra",
            $"Deseja remover \"{item.Title}\" da biblioteca do Quadra?",
            "Remover",
            "Cancelar");

        if (!confirmou)
            return;

        try
        {
            await _cleanupService.DeleteAsync(item);
            Itens.Remove(item);
            AtualizarApresentacao();

            await Shell.Current.DisplayAlertAsync(
                "Obra removida",
                "A cópia interna, a capa e os arquivos temporários foram removidos. O arquivo original não foi apagado.",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro ao remover", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task CarregarBibliotecaAsync()
    {
        if (EstaCarregandoBiblioteca)
            return;

        EstaCarregandoBiblioteca = true;
        TemErro = false;
        MensagemErro = string.Empty;
        AtualizarEstadoBiblioteca();

        try
        {
            var itensSalvos = await _database.GetLibraryItemsAsync();

            Itens.Clear();
            foreach (var item in itensSalvos)
                Itens.Add(item);

            AtualizarApresentacao();
        }
        catch (Exception ex)
        {
            TemErro = true;
            MensagemErro = "Não foi possível carregar sua biblioteca.";
            await Shell.Current.DisplayAlertAsync(
                "Erro ao carregar biblioteca",
                ex.Message,
                "OK");
        }
        finally
        {
            EstaCarregandoBiblioteca = false;
            AtualizarEstadoBiblioteca();
        }
    }

    [RelayCommand]
    private async Task AbrirDetalhesAsync(LibraryItem? item)
    {
        if (item is null)
            return;

        await Shell.Current.GoToAsync(
            nameof(BookDetailsPage),
            new Dictionary<string, object> { ["Item"] = item });
    }

    [RelayCommand]
    private void SelecionarFiltro(LibraryFormatFilter filter)
    {
        FiltroSelecionado = filter;
    }

    [RelayCommand]
    private void MostrarFiltros()
    {
        FiltrosVisiveis = !FiltrosVisiveis;
    }

    [RelayCommand]
    private void VoltarParaTodos()
    {
        FiltroSelecionado = LibraryFormatFilter.All;
    }

    private void AtualizarApresentacao()
    {
        var itensVisuais = Itens.Select(item => new LibraryBookViewData(item)).ToList();
        var filtrados = LibraryPresentationLogic.Filter(
            itensVisuais,
            FiltroSelecionado,
            item => item.Format);

        ItensFiltrados.Clear();
        foreach (var item in filtrados)
            ItensFiltrados.Add(item);

        var itemContinuar = LibraryPresentationLogic.SelectContinueReading(
            Itens,
            item => item.CurrentPage,
            item => item.TotalPages,
            item => item.LastReadAt);

        ContinuarLendo = itemContinuar is null
            ? null
            : new LibraryBookViewData(itemContinuar);

        AtualizarEstadoBiblioteca();
    }

    private void AtualizarEstadoBiblioteca()
    {
        BibliotecaVazia = !EstaCarregandoBiblioteca && !TemErro && Itens.Count == 0;
        BibliotecaPreenchida = !EstaCarregandoBiblioteca && !TemErro && Itens.Count > 0;
        PossuiItens = !EstaCarregandoBiblioteca && ItensFiltrados.Count > 0;
        FiltroSemResultados = !EstaCarregandoBiblioteca &&
                              Itens.Count > 0 &&
                              ItensFiltrados.Count == 0;
    }

    private static string FilterDescription(string label, bool selected)
    {
        return selected
            ? $"Filtro {label}, selecionado"
            : $"Filtrar por {label}";
    }
}
