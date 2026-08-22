using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Presentation;
using Quadra.App.Services.Readers;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public partial class LeitorViewModel : ObservableObject, IQueryAttributable
{
    private const string ChavePreferenciaNavegacaoToque = "ReaderTapNavigationEnabled";
    private static readonly TimeSpan AutoHideDelay = TimeSpan.FromSeconds(4);

    private readonly LeitorQuadrinhosService _leitorQuadrinhosService;
    private readonly QuadraDatabase _database;
    private readonly SemaphoreSlim _progressLock = new(1, 1);
    private readonly EstadoFocoLeitor _focusState = new();
    private readonly CoordenadorFechamentoLeitor _closeCoordinator = new();
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _progressCancellation;
    private CancellationTokenSource? _autoHideCancellation;
    private int _progressVersion;
    private int _isClosing;
    private int _navigationStarted;

    public ObservableCollection<ComicPage> Paginas { get; } = [];

    [ObservableProperty]
    public partial ObraBiblioteca? Item { get; set; }

    [ObservableProperty]
    public partial int PaginaAtual { get; set; }

    [ObservableProperty]
    public partial bool EstaCarregando { get; set; }

    [ObservableProperty]
    public partial bool ControlesVisiveis { get; set; } = true;

    [ObservableProperty]
    public partial bool ConfiguracoesVisiveis { get; set; }

    [ObservableProperty]
    public partial string TextoPagina { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool PodeVoltarPagina { get; set; }

    [ObservableProperty]
    public partial bool PodeAvancarPagina { get; set; }

    [ObservableProperty]
    public partial int IndiceMaximo { get; set; }

    [ObservableProperty]
    public partial double ProgressoNormalizado { get; set; }

    [ObservableProperty]
    public partial bool NavegacaoPorToqueAtivada { get; set; } = true;

    [ObservableProperty]
    public partial bool TemErro { get; set; }

    [ObservableProperty]
    public partial bool SemPaginas { get; set; }

    [ObservableProperty]
    public partial string MensagemErro { get; set; } = string.Empty;

    public LeitorViewModel(
        LeitorQuadrinhosService leitorQuadrinhosService,
        QuadraDatabase database)
    {
        _leitorQuadrinhosService = leitorQuadrinhosService;
        _database = database;

        try
        {
            NavegacaoPorToqueAtivada = Preferences.Default.Get(
                ChavePreferenciaNavegacaoToque,
                true);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    public bool ControlesOcultos => !ControlesVisiveis;
    public bool LeituraVisivel => !EstaCarregando && !TemErro && !SemPaginas && Paginas.Count > 0;
    public bool SomenteDeslizarSelecionado => !NavegacaoPorToqueAtivada;
    public bool DeslizarETocarSelecionado => NavegacaoPorToqueAtivada;
    public string DescricaoContador => string.IsNullOrEmpty(TextoPagina)
        ? "Nenhuma página disponível"
        : $"Página {TextoPagina.Replace("/", "de")}";

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("Item", out var value) || value is not ObraBiblioteca libraryItem)
        {
            TemErro = true;
            MensagemErro = "Não foi possível abrir esta leitura.";
            NotificarEstadoLeitor();
            return;
        }

        Item = libraryItem;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        System.Diagnostics.Debug.WriteLine(
            $"[CarregamentoLeitor] Nova abertura; Formato={Item.Format}; Caminho={Item.FilePath}; " +
            $"Cancelado={_loadCancellation.IsCancellationRequested}; PosicaoInicial={Item.CurrentPage}");
        await CarregarPaginasAsync(_loadCancellation.Token);
    }

    public void AtivarModoFoco()
    {
        if (IsClosing)
            return;

        _focusState.ExibirControles();
        SyncFocusState();
        ScheduleAutoHide();
    }

    public void RegistrarInteracao()
    {
        if (IsClosing)
            return;

        _focusState.ExibirControles();
        SyncFocusState();

        if (!_focusState.ConfiguracoesVisiveis)
            ScheduleAutoHide();
    }

    public void DefinirPaginaPeloSlider(double sliderValue)
    {
        if (Paginas.Count == 0 || IsClosing)
            return;

        PaginaAtual = LogicaApresentacaoLeitor.CriarEstadoPagina(
            (int)Math.Round(sliderValue),
            Paginas.Count).Indice;
        RegistrarInteracao();
    }

    public Task FecharAsync()
    {
        Interlocked.Exchange(ref _isClosing, 1);
        return _closeCoordinator.FecharAsync(
            PersistirProgressoAsync,
            CancelarCarregamento,
            CancelarOcultacaoAutomatica);
    }

    public void CancelarCarregamento() => _loadCancellation?.Cancel();
    public void CancelarOcultacaoAutomatica() => _autoHideCancellation?.Cancel();

    partial void OnPaginaAtualChanged(int value)
    {
        var pageState = LogicaApresentacaoLeitor.CriarEstadoPagina(value, Paginas.Count);
        if (value != pageState.Indice)
        {
            PaginaAtual = pageState.Indice;
            return;
        }

        ApplyPageState(pageState);
        if (!IsClosing && Paginas.Count > 0)
            EnfileirarSalvamento(value);
    }

    partial void OnEstaCarregandoChanged(bool value) => NotificarEstadoLeitor();
    partial void OnTemErroChanged(bool value) => NotificarEstadoLeitor();
    partial void OnSemPaginasChanged(bool value) => NotificarEstadoLeitor();

    partial void OnControlesVisiveisChanged(bool value)
    {
        OnPropertyChanged(nameof(ControlesOcultos));
    }

    partial void OnTextoPaginaChanged(string value)
    {
        OnPropertyChanged(nameof(DescricaoContador));
    }

    partial void OnNavegacaoPorToqueAtivadaChanged(bool value)
    {
        OnPropertyChanged(nameof(SomenteDeslizarSelecionado));
        OnPropertyChanged(nameof(DeslizarETocarSelecionado));
    }

    private bool IsClosing => Volatile.Read(ref _isClosing) != 0;

    private async Task CarregarPaginasAsync(CancellationToken cancellationToken)
    {
        if (Item is null || IsClosing)
            return;

        try
        {
            EstaCarregando = true;
            TemErro = false;
            SemPaginas = false;
            MensagemErro = string.Empty;

            var paginas = await _leitorQuadrinhosService.CarregarPaginasAsync(Item, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (IsClosing)
                return;

            Paginas.Clear();
            foreach (var pagina in paginas)
                Paginas.Add(pagina);

            if (Paginas.Count == 0)
            {
                SemPaginas = true;
                MensagemErro = "Nenhuma página válida foi encontrada neste arquivo.";
                ApplyPageState(LogicaApresentacaoLeitor.CriarEstadoPagina(0, 0));
                return;
            }

            var initialState = LogicaApresentacaoLeitor.CriarEstadoPagina(
                Item.CurrentPage,
                Paginas.Count);
            PaginaAtual = initialState.Indice;
            ApplyPageState(initialState);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[CarregamentoLeitor] Preparação cancelada; Formato={Item.Format}; Caminho={Item.FilePath}; " +
                $"Cancelado={cancellationToken.IsCancellationRequested}; Paginas={Paginas.Count}; " +
                $"PosicaoInicial={Item.CurrentPage}; Fechando={IsClosing}");
        }
        catch (FileNotFoundException exception)
        {
            LogLoadFailure(exception, cancellationToken);
            if (!IsClosing)
            {
                TemErro = true;
                MensagemErro = "O arquivo desta obra não foi encontrado.";
            }
        }
        catch (Exception exception)
        {
            LogLoadFailure(exception, cancellationToken);
            if (!IsClosing)
            {
                TemErro = true;
                MensagemErro = "Não foi possível preparar este arquivo para leitura.";
            }
        }
        finally
        {
            if (!IsClosing)
                EstaCarregando = false;
        }
    }

    private void ApplyPageState(EstadoPaginaLeitor state)
    {
        TextoPagina = state.TextoContador;
        PodeVoltarPagina = state.PodeVoltar;
        PodeAvancarPagina = state.PodeAvancar;
        IndiceMaximo = state.IndiceMaximo;
        ProgressoNormalizado = state.Progresso;
        NotificarEstadoLeitor();
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

                Item.CurrentPage = LogicaApresentacaoLeitor.CriarEstadoPagina(
                    pagina,
                    Paginas.Count).Indice;
                Item.TotalPages = Paginas.Count;
                Item.LastReadAt = DateTime.Now;
                await _database.SalvarObraBibliotecaAsync(Item);
            }
            finally
            {
                _progressLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Uma posição mais nova substituiu esta gravação.
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Não foi possível salvar o progresso: {exception}");
        }
    }

    public async Task PersistirProgressoAsync()
    {
        _progressCancellation?.Cancel();
        var version = Interlocked.Increment(ref _progressVersion);
        await SalvarProgressoSeguroAsync(
            PaginaAtual,
            version,
            debounce: false,
            CancellationToken.None);
    }

    [RelayCommand]
    private void AlternarControles()
    {
        _focusState.AlternarControles();
        SyncFocusState();

        if (_focusState.ControlesVisiveis)
            ScheduleAutoHide();
        else
            CancelarOcultacaoAutomatica();
    }

    [RelayCommand]
    private void AbrirConfiguracoes()
    {
        CancelarOcultacaoAutomatica();
        _focusState.AbrirConfiguracoes();
        SyncFocusState();
    }

    [RelayCommand]
    private void FecharConfiguracoes()
    {
        _focusState.FecharConfiguracoes();
        SyncFocusState();
        ScheduleAutoHide();
    }

    [RelayCommand]
    private void SelecionarSomenteDeslizar()
    {
        SaveNavigationPreference(false);
        RegistrarInteracao();
    }

    [RelayCommand]
    private void SelecionarDeslizarETocar()
    {
        SaveNavigationPreference(true);
        RegistrarInteracao();
    }

    [RelayCommand]
    private void AvancarPagina()
    {
        if (!PodeAvancarPagina || IsClosing)
            return;

        PaginaAtual++;
        RegistrarInteracao();
    }

    [RelayCommand]
    private void VoltarPagina()
    {
        if (!PodeVoltarPagina || IsClosing)
            return;

        PaginaAtual--;
        RegistrarInteracao();
    }

    [RelayCommand]
    private async Task VoltarAsync()
    {
        if (Interlocked.Exchange(ref _navigationStarted, 1) != 0)
            return;

        await FecharAsync();
        await Shell.Current.GoToAsync("..");
    }

    private void SaveNavigationPreference(bool enabled)
    {
        NavegacaoPorToqueAtivada = enabled;
        try
        {
            Preferences.Default.Set(ChavePreferenciaNavegacaoToque, enabled);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    private void SyncFocusState()
    {
        ControlesVisiveis = _focusState.ControlesVisiveis;
        ConfiguracoesVisiveis = _focusState.ConfiguracoesVisiveis;
    }

    private void ScheduleAutoHide()
    {
        CancelarOcultacaoAutomatica();
        if (IsClosing || _focusState.ConfiguracoesVisiveis || !_focusState.ControlesVisiveis)
            return;

        _autoHideCancellation?.Dispose();
        _autoHideCancellation = new CancellationTokenSource();
        _ = AutoHideAsync(_autoHideCancellation.Token);
    }

    private async Task AutoHideAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutoHideDelay, cancellationToken);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (IsClosing || _focusState.ConfiguracoesVisiveis)
                    return;

                _focusState.OcultarControles();
                SyncFocusState();
            });
        }
        catch (OperationCanceledException)
        {
            // Uma interação reiniciou o temporizador.
        }
    }

    private void NotificarEstadoLeitor()
    {
        OnPropertyChanged(nameof(LeituraVisivel));
    }

    private void LogLoadFailure(
        Exception exception,
        CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[CarregamentoLeitor] Tipo={exception.GetType().FullName}; Mensagem={exception.Message}; " +
            $"StackTrace={exception.StackTrace}; InnerException={exception.InnerException}; " +
            $"Formato={Item?.Format}; Caminho={Item?.FilePath}; " +
            $"Cancelado={cancellationToken.IsCancellationRequested}; Paginas={Paginas.Count}; " +
            $"PosicaoInicial={Item?.CurrentPage}; Fechando={IsClosing}; " +
            $"CloseCoordinatorAcionado={IsClosing}");
    }
}
