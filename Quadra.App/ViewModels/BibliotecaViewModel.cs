using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Pages;
using Quadra.App.Presentation;
using Quadra.App.Services.Covers;
using Quadra.App.Services.Import;
using Quadra.App.Services.Storage;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public partial class BibliotecaViewModel : ObservableObject
{
    private const string SortPreferenceKey = "library_sort_option";
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(275);

    private readonly ArmazenamentoBibliotecaService _storageService;
    private readonly QuadraDatabase _database;
    private readonly CapaService _coverService;
    private readonly LimpezaBibliotecaService _cleanupService;
    private readonly List<DadosObraBiblioteca> _visualItems = [];
    private CancellationTokenSource? _importCancellation;
    private CancellationTokenSource? _searchCancellation;
    private bool _suppressPipeline;
    private bool _suppressSearchDebounce;

    public ObservableCollection<ObraBiblioteca> Itens { get; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<DadosObraBiblioteca> ItensFiltrados { get; set; } = [];

    [ObservableProperty]
    public partial string? NomeArquivoSelecionado { get; set; }

    [ObservableProperty]
    public partial bool PossuiArquivoSelecionado { get; set; }

    [ObservableProperty]
    public partial bool EstaImportando { get; set; }

    [ObservableProperty]
    public partial ObraBiblioteca? ItemImportado { get; set; }

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
    public partial FiltroFormatoBiblioteca FiltroSelecionado { get; set; } = FiltroFormatoBiblioteca.Todos;

    [ObservableProperty]
    public partial FiltroStatusLeituraBiblioteca StatusSelecionado { get; set; } = FiltroStatusLeituraBiblioteca.Todos;

    [ObservableProperty]
    public partial OpcaoOrdenacaoBiblioteca OrdenacaoSelecionada { get; set; } = OpcaoOrdenacaoBiblioteca.ImportadasRecentemente;

    [ObservableProperty]
    public partial bool PainelFiltrosAberto { get; set; }

    [ObservableProperty]
    public partial FiltroFormatoBiblioteca FiltroTemporario { get; set; } = FiltroFormatoBiblioteca.Todos;

    [ObservableProperty]
    public partial FiltroStatusLeituraBiblioteca StatusTemporario { get; set; } = FiltroStatusLeituraBiblioteca.Todos;

    [ObservableProperty]
    public partial OpcaoOrdenacaoBiblioteca OrdenacaoTemporaria { get; set; } = OpcaoOrdenacaoBiblioteca.ImportadasRecentemente;

    [ObservableProperty]
    public partial DadosObraBiblioteca? ContinuarLendo { get; set; }

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

    public BibliotecaViewModel(
        ArmazenamentoBibliotecaService storageService,
        QuadraDatabase database,
        CapaService coverService,
        LimpezaBibliotecaService cleanupService)
    {
        _storageService = storageService;
        _database = database;
        _coverService = coverService;
        _cleanupService = cleanupService;

        try
        {
            OrdenacaoSelecionada = LogicaApresentacaoBiblioteca.InterpretarOpcaoOrdenacao(
                Preferences.Default.Get(
                    SortPreferenceKey,
                    (int)OpcaoOrdenacaoBiblioteca.ImportadasRecentemente));
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            OrdenacaoSelecionada = OpcaoOrdenacaoBiblioteca.ImportadasRecentemente;
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

    public bool FiltroTodosSelecionado => FiltroSelecionado == FiltroFormatoBiblioteca.Todos;
    public bool FiltroEpubSelecionado => FiltroSelecionado == FiltroFormatoBiblioteca.Epub;
    public bool FiltroPdfSelecionado => FiltroSelecionado == FiltroFormatoBiblioteca.Pdf;
    public bool FiltroComicsSelecionado => FiltroSelecionado == FiltroFormatoBiblioteca.Quadrinhos;
    public string DescricaoFiltroTodos => FilterDescription("Todos", FiltroTodosSelecionado);
    public string DescricaoFiltroEpub => FilterDescription("EPUB", FiltroEpubSelecionado);
    public string DescricaoFiltroPdf => FilterDescription("PDF", FiltroPdfSelecionado);
    public string DescricaoFiltroComics => FilterDescription("CBR e CBZ", FiltroComicsSelecionado);

    public bool FormatoTempTodos => FiltroTemporario == FiltroFormatoBiblioteca.Todos;
    public bool FormatoTempComics => FiltroTemporario == FiltroFormatoBiblioteca.Quadrinhos;
    public bool FormatoTempPdf => FiltroTemporario == FiltroFormatoBiblioteca.Pdf;
    public bool FormatoTempEpub => FiltroTemporario == FiltroFormatoBiblioteca.Epub;
    public bool StatusTempTodos => StatusTemporario == FiltroStatusLeituraBiblioteca.Todos;
    public bool StatusTempNaoIniciado => StatusTemporario == FiltroStatusLeituraBiblioteca.NaoIniciada;
    public bool StatusTempEmAndamento => StatusTemporario == FiltroStatusLeituraBiblioteca.EmAndamento;
    public bool StatusTempConcluido => StatusTemporario == FiltroStatusLeituraBiblioteca.Concluida;
    public bool SortTempRecentes => OrdenacaoTemporaria == OpcaoOrdenacaoBiblioteca.ImportadasRecentemente;
    public bool SortTempUltimaLeitura => OrdenacaoTemporaria == OpcaoOrdenacaoBiblioteca.UltimaLeitura;
    public bool SortTempTituloAsc => OrdenacaoTemporaria == OpcaoOrdenacaoBiblioteca.TituloCrescente;
    public bool SortTempTituloDesc => OrdenacaoTemporaria == OpcaoOrdenacaoBiblioteca.TituloDecrescente;
    public bool SortTempProgressoAsc => OrdenacaoTemporaria == OpcaoOrdenacaoBiblioteca.ProgressoCrescente;
    public bool SortTempProgressoDesc => OrdenacaoTemporaria == OpcaoOrdenacaoBiblioteca.ProgressoDecrescente;

    partial void OnEstaImportandoChanged(bool value)
    {
        OnPropertyChanged(nameof(PodeImportar));
    }

    partial void OnBuscaVisivelChanged(bool value)
    {
        OnPropertyChanged(nameof(CabecalhoPadraoVisivel));
    }

    partial void OnContinuarLendoChanged(DadosObraBiblioteca? value)
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

    partial void OnFiltroSelecionadoChanged(FiltroFormatoBiblioteca value)
    {
        NotifyAppliedFilterState();
        if (!_suppressPipeline)
            AtualizarApresentacao();
    }

    partial void OnStatusSelecionadoChanged(FiltroStatusLeituraBiblioteca value)
    {
        NotifyAppliedFilterState();
        if (!_suppressPipeline)
            AtualizarApresentacao();
    }

    partial void OnOrdenacaoSelecionadaChanged(OpcaoOrdenacaoBiblioteca value)
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

            ItemImportado = await _storageService.ImportarAsync(arquivo, cancellationToken);
            ItemImportado.CoverPath = await _coverService.GerarCapaAsync(
                ItemImportado,
                cancellationToken);
            await _database.SalvarObraBibliotecaAsync(ItemImportado);

            Itens.Insert(0, ItemImportado);
            _visualItems.Insert(0, new DadosObraBiblioteca(ItemImportado));
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

    public void CancelarImportacao() => _importCancellation?.Cancel();
    public void CancelarBuscaPendente() => _searchCancellation?.Cancel();

    private async Task CleanupIncompleteImportAsync()
    {
        if (ItemImportado is null || ItemImportado.Id > 0)
            return;

        await _storageService.ExcluirAsync(ItemImportado);
        ItemImportado = null;
    }

    [RelayCommand]
    private async Task ExcluirItemAsync(ObraBiblioteca? item)
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
            await _cleanupService.ExcluirAsync(item);
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
            var itensSalvos = await _database.ObterObrasBibliotecaAsync();
            Itens.Clear();
            _visualItems.Clear();
            foreach (var item in itensSalvos)
            {
                Itens.Add(item);
                _visualItems.Add(new DadosObraBiblioteca(item));
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
    private async Task AbrirDetalhesAsync(ObraBiblioteca? item)
    {
        if (item is null)
            return;

        await Shell.Current.GoToAsync(
            "BookDetailsPage",
            new Dictionary<string, object> { ["Item"] = item });
    }

    [RelayCommand]
    private void SelecionarFiltro(FiltroFormatoBiblioteca filter)
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
    private void SelecionarFormatoTemporario(FiltroFormatoBiblioteca filter)
    {
        FiltroTemporario = filter;
        NotifyTemporaryFilterState();
    }

    [RelayCommand]
    private void SelecionarStatusTemporario(FiltroStatusLeituraBiblioteca status)
    {
        StatusTemporario = status;
        NotifyTemporaryFilterState();
    }

    [RelayCommand]
    private void SelecionarOrdenacaoTemporaria(OpcaoOrdenacaoBiblioteca sort)
    {
        OrdenacaoTemporaria = sort;
        NotifyTemporaryFilterState();
    }

    [RelayCommand]
    private void LimparFiltrosTemporarios()
    {
        FiltroTemporario = FiltroFormatoBiblioteca.Todos;
        StatusTemporario = FiltroStatusLeituraBiblioteca.Todos;
        OrdenacaoTemporaria = OpcaoOrdenacaoBiblioteca.ImportadasRecentemente;
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
        FiltroSelecionado = FiltroFormatoBiblioteca.Todos;
        StatusSelecionado = FiltroStatusLeituraBiblioteca.Todos;
        OrdenacaoSelecionada = OpcaoOrdenacaoBiblioteca.ImportadasRecentemente;
        _suppressPipeline = false;
        _suppressSearchDebounce = false;
        PersistSortPreference();
        NotifyAppliedFilterState();
        AtualizarApresentacao();
    }

    [RelayCommand]
    private void LimparFormato()
    {
        FiltroSelecionado = FiltroFormatoBiblioteca.Todos;
    }

    [RelayCommand]
    private void LimparStatus()
    {
        StatusSelecionado = FiltroStatusLeituraBiblioteca.Todos;
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
        ItensFiltrados = LogicaApresentacaoBiblioteca.AplicarPipeline(
            _visualItems,
            new CriteriosFiltroBiblioteca(
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

        var itemContinuar = LogicaApresentacaoBiblioteca.SelecionarContinuarLeitura(
            Itens,
            item => item.CurrentPage,
            item => item.TotalPages,
            item => item.LastReadAt);
        ContinuarLendo = itemContinuar is null
            ? null
            : _visualItems.FirstOrDefault(data => data.Item.Id == itemContinuar.Id);

        ContagemFiltrosAtivos = LogicaApresentacaoBiblioteca.ContarFiltrosAtivos(
            FiltroSelecionado,
            StatusSelecionado,
            OrdenacaoSelecionada);
        PossuiFiltrosAtivos = ContagemFiltrosAtivos > 0;
        BuscaAtiva = !string.IsNullOrWhiteSpace(TextoBusca);
        DescricaoResultados = ItensFiltrados.Count == 1
            ? "1 obra encontrada"
            : $"{ItensFiltrados.Count} obras encontradas";

        var onlyFormat = FiltroSelecionado != FiltroFormatoBiblioteca.Todos &&
                         StatusSelecionado == FiltroStatusLeituraBiblioteca.Todos &&
                         OrdenacaoSelecionada == OpcaoOrdenacaoBiblioteca.ImportadasRecentemente &&
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
                          LogicaApresentacaoBiblioteca.BibliotecaEstaVazia(Itens.Count);
        FiltroSemResultados = !EstaCarregandoBiblioteca &&
                              !TemErro &&
                              LogicaApresentacaoBiblioteca.FiltroEstaVazio(
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
