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
    private const string SortPreferenceKey = "library_sort_option";
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(275);

    private readonly LibraryStorageService _storageService;
    private readonly QuadraDatabase _database;
    private readonly CoverService _coverService;
    private readonly LibraryCleanupService _cleanupService;
    private readonly List<LibraryBookViewData> _visualItems = [];
    private CancellationTokenSource? _importCancellation;
    private CancellationTokenSource? _searchCancellation;
    private bool _suppressPipeline;
    private bool _suppressSearchDebounce;

    public ObservableCollection<LibraryItem> Itens { get; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<LibraryBookViewData> ItensFiltrados { get; set; } = [];

    [ObservableProperty]
    public partial string? NomeArquivoSelecionado { get; set; }

    [ObservableProperty]
    public partial bool PossuiArquivoSelecionado { get; set; }

    [ObservableProperty]
    public partial bool EstaImportando { get; set; }

    [ObservableProperty]
    public partial LibraryItem? ItemImportado { get; set; }

    [ObservableProperty]
    public partial bool BibliotecaVazia { get; set; }

    [ObservableProperty]
    public partial bool PossuiItens { get; set; }

    [ObservableProperty]
    public partial bool EstaCarregandoBiblioteca { get; set; }

    [ObservableProperty]
    public partial bool BibliotecaPreenchida { get; set; }

    [ObservableProperty]
    public partial bool FiltroSemResultados { get; set; }

    [ObservableProperty]
    public partial bool TemErro { get; set; }

    [ObservableProperty]
    public partial string MensagemErro { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool BuscaVisivel { get; set; }

    [ObservableProperty]
    public partial string TextoBusca { get; set; } = string.Empty;

    [ObservableProperty]
    public partial LibraryFormatFilter FiltroSelecionado { get; set; } = LibraryFormatFilter.All;

    [ObservableProperty]
    public partial LibraryReadingStatusFilter StatusSelecionado { get; set; } = LibraryReadingStatusFilter.All;

    [ObservableProperty]
    public partial LibrarySortOption OrdenacaoSelecionada { get; set; } = LibrarySortOption.RecentlyImported;

    [ObservableProperty]
    public partial bool PainelFiltrosAberto { get; set; }

    [ObservableProperty]
    public partial LibraryFormatFilter FiltroTemporario { get; set; } = LibraryFormatFilter.All;

    [ObservableProperty]
    public partial LibraryReadingStatusFilter StatusTemporario { get; set; } = LibraryReadingStatusFilter.All;

    [ObservableProperty]
    public partial LibrarySortOption OrdenacaoTemporaria { get; set; } = LibrarySortOption.RecentlyImported;

    [ObservableProperty]
    public partial LibraryBookViewData? ContinuarLendo { get; set; }

    [ObservableProperty]
    public partial int ContagemFiltrosAtivos { get; set; }

    [ObservableProperty]
    public partial bool PossuiFiltrosAtivos { get; set; }

    [ObservableProperty]
    public partial bool BuscaAtiva { get; set; }

    [ObservableProperty]
    public partial string TituloSemResultados { get; set; } = "Nenhuma obra encontrada";

    [ObservableProperty]
    public partial string DescricaoSemResultados { get; set; } = "Tente alterar a pesquisa ou os filtros.";

    [ObservableProperty]
    public partial string DescricaoResultados { get; set; } = "Nenhuma obra encontrada";

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

        try
        {
            OrdenacaoSelecionada = LibraryPresentationLogic.ParseSortOption(
                Preferences.Default.Get(
                    SortPreferenceKey,
                    (int)LibrarySortOption.RecentlyImported));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            OrdenacaoSelecionada = LibrarySortOption.RecentlyImported;
        }
    }

    public bool PodeImportar => !EstaImportando;
    public bool PossuiContinuarLendo => ContinuarLendo is not null;
    public bool CabecalhoPadraoVisivel => !BuscaVisivel;
    public bool PodeLimparPesquisa => BuscaAtiva;
    public bool PodeLimparFiltros => BuscaAtiva || PossuiFiltrosAtivos;
    public string TextoBadgeFiltros => ContagemFiltrosAtivos.ToString();
    public string DescricaoBotaoFiltros => PossuiFiltrosAtivos
        ? $"Abrir filtros. {ContagemFiltrosAtivos} critérios ativos."
        : "Abrir filtros da biblioteca";

    public bool FiltroTodosSelecionado => FiltroSelecionado == LibraryFormatFilter.All;
    public bool FiltroEpubSelecionado => FiltroSelecionado == LibraryFormatFilter.Epub;
    public bool FiltroPdfSelecionado => FiltroSelecionado == LibraryFormatFilter.Pdf;
    public bool FiltroComicsSelecionado => FiltroSelecionado == LibraryFormatFilter.Comics;
    public string DescricaoFiltroTodos => FilterDescription("Todos", FiltroTodosSelecionado);
    public string DescricaoFiltroEpub => FilterDescription("EPUB", FiltroEpubSelecionado);
    public string DescricaoFiltroPdf => FilterDescription("PDF", FiltroPdfSelecionado);
    public string DescricaoFiltroComics => FilterDescription("CBR e CBZ", FiltroComicsSelecionado);

    public bool FormatoTempTodos => FiltroTemporario == LibraryFormatFilter.All;
    public bool FormatoTempComics => FiltroTemporario == LibraryFormatFilter.Comics;
    public bool FormatoTempPdf => FiltroTemporario == LibraryFormatFilter.Pdf;
    public bool FormatoTempEpub => FiltroTemporario == LibraryFormatFilter.Epub;
    public bool StatusTempTodos => StatusTemporario == LibraryReadingStatusFilter.All;
    public bool StatusTempNaoIniciado => StatusTemporario == LibraryReadingStatusFilter.NotStarted;
    public bool StatusTempEmAndamento => StatusTemporario == LibraryReadingStatusFilter.InProgress;
    public bool StatusTempConcluido => StatusTemporario == LibraryReadingStatusFilter.Completed;
    public bool SortTempRecentes => OrdenacaoTemporaria == LibrarySortOption.RecentlyImported;
    public bool SortTempUltimaLeitura => OrdenacaoTemporaria == LibrarySortOption.LastRead;
    public bool SortTempTituloAsc => OrdenacaoTemporaria == LibrarySortOption.TitleAscending;
    public bool SortTempTituloDesc => OrdenacaoTemporaria == LibrarySortOption.TitleDescending;
    public bool SortTempProgressoAsc => OrdenacaoTemporaria == LibrarySortOption.ProgressAscending;
    public bool SortTempProgressoDesc => OrdenacaoTemporaria == LibrarySortOption.ProgressDescending;

    partial void OnEstaImportandoChanged(bool value)
    {
        OnPropertyChanged(nameof(PodeImportar));
    }

    partial void OnBuscaVisivelChanged(bool value)
    {
        OnPropertyChanged(nameof(CabecalhoPadraoVisivel));
    }

    partial void OnContinuarLendoChanged(LibraryBookViewData? value)
    {
        OnPropertyChanged(nameof(PossuiContinuarLendo));
    }

    partial void OnTextoBuscaChanged(string value)
    {
        if (_suppressSearchDebounce)
            return;

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        _ = ApplySearchWithDebounceAsync(_searchCancellation.Token);
    }

    partial void OnFiltroSelecionadoChanged(LibraryFormatFilter value)
    {
        NotifyAppliedFilterState();
        if (!_suppressPipeline)
            AtualizarApresentacao();
    }

    partial void OnStatusSelecionadoChanged(LibraryReadingStatusFilter value)
    {
        NotifyAppliedFilterState();
        if (!_suppressPipeline)
            AtualizarApresentacao();
    }

    partial void OnOrdenacaoSelecionadaChanged(LibrarySortOption value)
    {
        NotifyAppliedFilterState();
        if (!_suppressPipeline)
            AtualizarApresentacao();
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

            ItemImportado = await _storageService.ImportAsync(arquivo, cancellationToken);
            ItemImportado.CoverPath = await _coverService.GenerateCoverAsync(
                ItemImportado,
                cancellationToken);
            await _database.SaveLibraryItemAsync(ItemImportado);

            Itens.Insert(0, ItemImportado);
            _visualItems.Insert(0, new LibraryBookViewData(ItemImportado));
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
        catch (Exception exception)
        {
            await CleanupIncompleteImportAsync();
            await Shell.Current.DisplayAlertAsync("Erro ao importar", exception.Message, "OK");
        }
        finally
        {
            EstaImportando = false;
        }
    }

    public void CancelImport() => _importCancellation?.Cancel();
    public void CancelPendingSearch() => _searchCancellation?.Cancel();

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
            _visualItems.RemoveAll(data => data.Item.Id == item.Id);
            AtualizarApresentacao();
            await Shell.Current.DisplayAlertAsync(
                "Obra removida",
                "A cópia interna, a capa e os arquivos temporários foram removidos. O arquivo original não foi apagado.",
                "OK");
        }
        catch (Exception exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro ao remover", exception.Message, "OK");
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
            _visualItems.Clear();
            foreach (var item in itensSalvos)
            {
                Itens.Add(item);
                _visualItems.Add(new LibraryBookViewData(item));
            }

            AtualizarApresentacao();
        }
        catch (Exception exception)
        {
            TemErro = true;
            MensagemErro = "Não foi possível carregar sua biblioteca.";
            await Shell.Current.DisplayAlertAsync(
                "Erro ao carregar biblioteca",
                exception.Message,
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
    private void AbrirBusca()
    {
        BuscaVisivel = true;
    }

    [RelayCommand]
    private void FecharBusca()
    {
        BuscaVisivel = false;
    }

    [RelayCommand]
    private void LimparBusca()
    {
        _searchCancellation?.Cancel();
        _suppressSearchDebounce = true;
        TextoBusca = string.Empty;
        _suppressSearchDebounce = false;
        AtualizarApresentacao();
    }

    [RelayCommand]
    private void AbrirPainelFiltros()
    {
        FiltroTemporario = FiltroSelecionado;
        StatusTemporario = StatusSelecionado;
        OrdenacaoTemporaria = OrdenacaoSelecionada;
        NotifyTemporaryFilterState();
        PainelFiltrosAberto = true;
    }

    [RelayCommand]
    private void FecharPainelFiltros()
    {
        PainelFiltrosAberto = false;
    }

    [RelayCommand]
    private void SelecionarFormatoTemporario(LibraryFormatFilter filter)
    {
        FiltroTemporario = filter;
        NotifyTemporaryFilterState();
    }

    [RelayCommand]
    private void SelecionarStatusTemporario(LibraryReadingStatusFilter status)
    {
        StatusTemporario = status;
        NotifyTemporaryFilterState();
    }

    [RelayCommand]
    private void SelecionarOrdenacaoTemporaria(LibrarySortOption sort)
    {
        OrdenacaoTemporaria = sort;
        NotifyTemporaryFilterState();
    }

    [RelayCommand]
    private void LimparFiltrosTemporarios()
    {
        FiltroTemporario = LibraryFormatFilter.All;
        StatusTemporario = LibraryReadingStatusFilter.All;
        OrdenacaoTemporaria = LibrarySortOption.RecentlyImported;
        NotifyTemporaryFilterState();
    }

    [RelayCommand]
    private void AplicarFiltros()
    {
        _suppressPipeline = true;
        FiltroSelecionado = FiltroTemporario;
        StatusSelecionado = StatusTemporario;
        OrdenacaoSelecionada = OrdenacaoTemporaria;
        _suppressPipeline = false;
        PersistSortPreference();
        PainelFiltrosAberto = false;
        AtualizarApresentacao();
    }

    [RelayCommand]
    private void LimparFiltrosBiblioteca()
    {
        _searchCancellation?.Cancel();
        _suppressSearchDebounce = true;
        _suppressPipeline = true;
        TextoBusca = string.Empty;
        FiltroSelecionado = LibraryFormatFilter.All;
        StatusSelecionado = LibraryReadingStatusFilter.All;
        OrdenacaoSelecionada = LibrarySortOption.RecentlyImported;
        _suppressPipeline = false;
        _suppressSearchDebounce = false;
        PersistSortPreference();
        NotifyAppliedFilterState();
        AtualizarApresentacao();
    }

    [RelayCommand]
    private void LimparFormato()
    {
        FiltroSelecionado = LibraryFormatFilter.All;
    }

    [RelayCommand]
    private void LimparStatus()
    {
        StatusSelecionado = LibraryReadingStatusFilter.All;
    }

    [RelayCommand]
    private void VoltarParaTodos()
    {
        LimparFiltrosBiblioteca();
    }

    private async Task ApplySearchWithDebounceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SearchDebounce, cancellationToken);
            await MainThread.InvokeOnMainThreadAsync(AtualizarApresentacao);
        }
        catch (OperationCanceledException)
        {
            // Uma busca mais recente substituiu esta atualização.
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    private void AtualizarApresentacao()
    {
        ItensFiltrados = LibraryPresentationLogic.ApplyPipeline(
            _visualItems,
            new LibraryFilterCriteria(
                TextoBusca,
                FiltroSelecionado,
                StatusSelecionado,
                OrdenacaoSelecionada),
            data => data.Item.Title,
            data => data.Item.OriginalFileName,
            data => data.Item.Format,
            data => data.Item.CurrentPage,
            data => data.Item.TotalPages,
            data => data.Item.ImportedAt,
            data => data.Item.LastReadAt);

        var itemContinuar = LibraryPresentationLogic.SelectContinueReading(
            Itens,
            item => item.CurrentPage,
            item => item.TotalPages,
            item => item.LastReadAt);
        ContinuarLendo = itemContinuar is null
            ? null
            : _visualItems.FirstOrDefault(data => data.Item.Id == itemContinuar.Id);

        ContagemFiltrosAtivos = LibraryPresentationLogic.CountActiveFilters(
            FiltroSelecionado,
            StatusSelecionado,
            OrdenacaoSelecionada);
        PossuiFiltrosAtivos = ContagemFiltrosAtivos > 0;
        BuscaAtiva = !string.IsNullOrWhiteSpace(TextoBusca);
        DescricaoResultados = ItensFiltrados.Count == 1
            ? "1 obra encontrada"
            : $"{ItensFiltrados.Count} obras encontradas";

        var onlyFormat = FiltroSelecionado != LibraryFormatFilter.All &&
                         StatusSelecionado == LibraryReadingStatusFilter.All &&
                         OrdenacaoSelecionada == LibrarySortOption.RecentlyImported &&
                         !BuscaAtiva;
        TituloSemResultados = onlyFormat
            ? "Nenhuma obra neste formato"
            : "Nenhuma obra encontrada";
        DescricaoSemResultados = onlyFormat
            ? "Escolha outro formato ou mostre todos os itens."
            : "Tente alterar a pesquisa ou os filtros.";

        NotifyAppliedFilterState();
        AtualizarEstadoBiblioteca();
    }

    private void AtualizarEstadoBiblioteca()
    {
        BibliotecaVazia = !EstaCarregandoBiblioteca &&
                          !TemErro &&
                          LibraryPresentationLogic.IsLibraryEmpty(Itens.Count);
        FiltroSemResultados = !EstaCarregandoBiblioteca &&
                              !TemErro &&
                              LibraryPresentationLogic.IsFilteredEmpty(
                                  Itens.Count,
                                  ItensFiltrados.Count);
        BibliotecaPreenchida = !EstaCarregandoBiblioteca &&
                               !TemErro &&
                               ItensFiltrados.Count > 0;
        PossuiItens = BibliotecaPreenchida;
    }

    private void NotifyAppliedFilterState()
    {
        OnPropertyChanged(nameof(FiltroTodosSelecionado));
        OnPropertyChanged(nameof(FiltroEpubSelecionado));
        OnPropertyChanged(nameof(FiltroPdfSelecionado));
        OnPropertyChanged(nameof(FiltroComicsSelecionado));
        OnPropertyChanged(nameof(DescricaoFiltroTodos));
        OnPropertyChanged(nameof(DescricaoFiltroEpub));
        OnPropertyChanged(nameof(DescricaoFiltroPdf));
        OnPropertyChanged(nameof(DescricaoFiltroComics));
        OnPropertyChanged(nameof(PodeLimparPesquisa));
        OnPropertyChanged(nameof(PodeLimparFiltros));
        OnPropertyChanged(nameof(TextoBadgeFiltros));
        OnPropertyChanged(nameof(DescricaoBotaoFiltros));
    }

    private void NotifyTemporaryFilterState()
    {
        OnPropertyChanged(nameof(FormatoTempTodos));
        OnPropertyChanged(nameof(FormatoTempComics));
        OnPropertyChanged(nameof(FormatoTempPdf));
        OnPropertyChanged(nameof(FormatoTempEpub));
        OnPropertyChanged(nameof(StatusTempTodos));
        OnPropertyChanged(nameof(StatusTempNaoIniciado));
        OnPropertyChanged(nameof(StatusTempEmAndamento));
        OnPropertyChanged(nameof(StatusTempConcluido));
        OnPropertyChanged(nameof(SortTempRecentes));
        OnPropertyChanged(nameof(SortTempUltimaLeitura));
        OnPropertyChanged(nameof(SortTempTituloAsc));
        OnPropertyChanged(nameof(SortTempTituloDesc));
        OnPropertyChanged(nameof(SortTempProgressoAsc));
        OnPropertyChanged(nameof(SortTempProgressoDesc));
    }

    private void PersistSortPreference()
    {
        try
        {
            Preferences.Default.Set(SortPreferenceKey, (int)OrdenacaoSelecionada);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    private static string FilterDescription(string label, bool selected)
    {
        return selected
            ? $"Filtro {label}, selecionado"
            : $"Filtrar por {label}";
    }
}
