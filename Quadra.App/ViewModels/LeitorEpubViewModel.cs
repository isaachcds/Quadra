using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Presentation;
using Quadra.App.Services.Readers;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Quadra.App.ViewModels;

public partial class LeitorEpubViewModel : ObservableObject, IQueryAttributable
{
    private static readonly TimeSpan AtrasoModoFoco = TimeSpan.FromSeconds(6);
    private readonly ILeitorEpubService _leitorEpubService;
    private readonly QuadraDatabase _database;
    private readonly EstadoFocoLeitor _estadoFoco = new();
    private readonly CoordenadorFechamentoLeitor _coordenadorFechamento = new();
    private readonly SemaphoreSlim _bloqueioProgresso = new(1, 1);
    private CancellationTokenSource? _cancelamentoCarregamento;
    private CancellationTokenSource? _cancelamentoOcultacao;
    private PreferenciasLeituraEpub _preferencias;
    private string? _htmlCapituloAtual;
    private int _estaFechando;
    private int _navegacaoIniciada;

    public string? ContentRoot { get; private set; }
    public ObservableCollection<EpubChapter> Capitulos { get; } = [];

    [ObservableProperty] public partial ObraBiblioteca? Item { get; set; }
    [ObservableProperty] public partial int CapituloAtual { get; set; }
    [ObservableProperty] public partial bool EstaCarregando { get; set; }
    [ObservableProperty] public partial bool ControlesVisiveis { get; set; } = true;
    [ObservableProperty] public partial bool PainelAparenciaVisivel { get; set; }
    [ObservableProperty] public partial string TextoCapitulo { get; set; } = string.Empty;
    [ObservableProperty] public partial string TituloCapitulo { get; set; } = string.Empty;
    [ObservableProperty] public partial WebViewSource? ConteudoCapitulo { get; set; }
    [ObservableProperty] public partial bool TemErro { get; set; }
    [ObservableProperty] public partial string MensagemErro { get; set; } = string.Empty;
    [ObservableProperty] public partial Color CorFundo { get; set; } = Colors.White;

    public LeitorEpubViewModel(ILeitorEpubService leitorEpubService, QuadraDatabase database)
    {
        _leitorEpubService = leitorEpubService;
        _database = database;
        _preferencias = CarregarPreferencias();
        AtualizarCoresLeitura();
    }

    public bool PodeVoltarCapitulo => CapituloAtual > 0 && !EstaCarregando;
    public bool PodeAvancarCapitulo => CapituloAtual < Capitulos.Count - 1 && !EstaCarregando;
    public bool ControlesOcultos => !ControlesVisiveis;
    public bool TemaClaroSelecionado => _preferencias.Tema == TemaLeituraEpub.Claro;
    public bool TemaEscuroSelecionado => _preferencias.Tema == TemaLeituraEpub.Escuro;
    public bool TemaSepiaSelecionado => _preferencias.Tema == TemaLeituraEpub.Sepia;
    public bool FonteSistemaSelecionada => _preferencias.Fonte == FonteLeituraEpub.Sistema;
    public bool FonteSansSerifSelecionada => _preferencias.Fonte == FonteLeituraEpub.SansSerif;
    public bool FonteSerifSelecionada => _preferencias.Fonte == FonteLeituraEpub.Serif;
    public bool AlinhamentoEsquerdaSelecionado => _preferencias.Alinhamento == AlinhamentoLeituraEpub.Esquerda;
    public bool AlinhamentoJustificadoSelecionado => _preferencias.Alinhamento == AlinhamentoLeituraEpub.Justificado;
    public double TamanhoTexto => _preferencias.TamanhoTexto;
    public double EspacamentoLinhas => _preferencias.EspacamentoLinhas;
    public double MargemLeitura => _preferencias.Margem;
    public string TextoTamanhoTexto => $"{TamanhoTexto:0} px";
    public string TextoEspacamentoLinhas => EspacamentoLinhas.ToString("0.0");
    public string TextoMargemLeitura => $"{MargemLeitura:0} px";
    public double ProgressoCapitulo => Capitulos.Count <= 1 ? (Capitulos.Count == 0 ? 0 : 1) : (double)(CapituloAtual + 1) / Capitulos.Count;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (EstaFechando || !query.TryGetValue("Item", out var value) || value is not ObraBiblioteca obra)
            return;

        Item = obra;
        ContentRoot = _leitorEpubService.ObterRaizConteudo(obra);
        _cancelamentoCarregamento?.Cancel();
        _cancelamentoCarregamento?.Dispose();
        _cancelamentoCarregamento = new CancellationTokenSource();
        await CarregarLivroAsync(_cancelamentoCarregamento.Token);
    }

    public void AtivarModoFoco()
    {
        if (EstaFechando) return;
        _estadoFoco.ExibirControles();
        SincronizarModoFoco();
        CancelarOcultacaoAutomatica();
    }

    public void RegistrarInteracao()
    {
        if (EstaFechando || PainelAparenciaVisivel) return;
        _estadoFoco.ExibirControles();
        SincronizarModoFoco();
        AgendarOcultacaoAutomatica();
    }

    public Task FecharAsync()
    {
        Interlocked.Exchange(ref _estaFechando, 1);
        return _coordenadorFechamento.FecharAsync(SalvarProgressoAsync, CancelarOperacoes, CancelarOcultacaoAutomatica);
    }

    public bool IsLocalNavigationAllowed(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.StartsWith('#')) return true;
        if (url.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.IsNullOrWhiteSpace(ContentRoot) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsFile) return false;
        return EpubPathResolver.IsInsideRoot(ContentRoot, uri.LocalPath);
    }

    public Task DefinirTemaAsync(string? valor) => AtualizarPreferenciasAsync(preferencias => preferencias with { Tema = InterpretarEnum(valor, preferencias.Tema) });
    public Task DefinirFonteAsync(string? valor) => AtualizarPreferenciasAsync(preferencias => preferencias with { Fonte = InterpretarEnum(valor, preferencias.Fonte) });
    public Task DefinirAlinhamentoAsync(string? valor) => AtualizarPreferenciasAsync(preferencias => preferencias with { Alinhamento = InterpretarEnum(valor, preferencias.Alinhamento) });
    public Task DefinirTamanhoTextoAsync(double valor) => AtualizarPreferenciasAsync(preferencias => preferencias with { TamanhoTexto = Math.Clamp(valor, AparenciaLeituraEpub.TamanhoTextoMinimo, AparenciaLeituraEpub.TamanhoTextoMaximo) });
    public Task DefinirEspacamentoLinhasAsync(double valor) => AtualizarPreferenciasAsync(preferencias => preferencias with { EspacamentoLinhas = Math.Clamp(valor, AparenciaLeituraEpub.EspacamentoLinhasMinimo, AparenciaLeituraEpub.EspacamentoLinhasMaximo) });
    public Task DefinirMargemLeituraAsync(double valor) => AtualizarPreferenciasAsync(preferencias => preferencias with { Margem = Math.Clamp(valor, AparenciaLeituraEpub.MargemMinima, AparenciaLeituraEpub.MargemMaxima) });

    private async Task CarregarLivroAsync(CancellationToken cancellationToken)
    {
        if (Item is null || EstaFechando) return;
        try
        {
            EstaCarregando = true;
            TemErro = false;
            MensagemErro = string.Empty;
            var capitulos = await _leitorEpubService.CarregarCapitulosAsync(Item, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (EstaFechando) return;
            Capitulos.Clear();
            foreach (var capitulo in capitulos) Capitulos.Add(capitulo);
            CapituloAtual = Math.Clamp(Item.CurrentPage, 0, Math.Max(0, Capitulos.Count - 1));
            await CarregarCapituloAtualAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) when (!EstaFechando)
        {
            TemErro = true;
            MensagemErro = exception.Message;
        }
        finally
        {
            if (!EstaFechando) EstaCarregando = false;
        }
    }

    private async Task CarregarCapituloAtualAsync(CancellationToken cancellationToken = default)
    {
        if (Capitulos.Count == 0 || EstaFechando) return;
        var indice = Math.Clamp(CapituloAtual, 0, Capitulos.Count - 1);
        var capitulo = Capitulos[indice];
        if (!File.Exists(capitulo.LocalFilePath)) throw new FileNotFoundException("O arquivo do capítulo não foi encontrado.", capitulo.LocalFilePath);
        var html = await File.ReadAllTextAsync(capitulo.LocalFilePath, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (EstaFechando) return;
        _htmlCapituloAtual = SanitizarDocumentoHtml(html, capitulo.LocalFilePath);
        ConteudoCapitulo = CriarFonteHtml(_htmlCapituloAtual);
        TituloCapitulo = capitulo.Title;
        AtualizarEstadoCapitulo();
        await SalvarProgressoAsync();
    }

    private string SanitizarDocumentoHtml(string html, string caminhoCapitulo)
    {
        if (string.IsNullOrWhiteSpace(ContentRoot)) return html;
        var semConteudoAtivo = Regex.Replace(html, "<script\\b[^>]*>.*?</script\\s*>|<(?:iframe|object|embed|base)\\b[^>]*>.*?</(?:iframe|object|embed)\\s*>|<(?:iframe|object|embed|base)\\b[^>]*/?>|<meta\\b[^>]*http-equiv\\s*=\\s*(['\\\"]?)refresh\\1[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        semConteudoAtivo = Regex.Replace(semConteudoAtivo, "\\son[a-z0-9_-]+\\s*=\\s*(?:\\\"[^\\\"]*\\\"|'[^']*'|[^\\s>]+)|\\ssrcset\\s*=\\s*(?:\\\"[^\\\"]*\\\"|'[^']*'|[^\\s>]+)", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var diretorioCapitulo = Path.GetDirectoryName(caminhoCapitulo) ?? ContentRoot;
        var referenciasSeguras = Regex.Replace(semConteudoAtivo, "(?<attribute>src|href)\\s*=\\s*(?:(?<quote>[\\\"'])(?<value>.*?)(\\k<quote>)|(?<value>[^\\s>]+))", match =>
        {
            var referencia = match.Groups["value"].Value;
            return string.IsNullOrWhiteSpace(referencia) || referencia.StartsWith('#') || EpubContentSanitizer.IsSafeReference(ContentRoot, diretorioCapitulo, referencia)
                ? match.Value : $"{match.Groups["attribute"].Value}=\"#\"";
        }, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return EpubContentSanitizer.SanitizeCssReferences(referenciasSeguras, ContentRoot, diretorioCapitulo);
    }

    private HtmlWebViewSource CriarFonteHtml(string html)
    {
        var capitulo = Capitulos.Count == 0 ? null : Capitulos[Math.Clamp(CapituloAtual, 0, Capitulos.Count - 1)];
        return new HtmlWebViewSource { Html = AplicarCssLeitura(html), BaseUrl = capitulo is null ? ContentRoot : Path.GetDirectoryName(capitulo.LocalFilePath) };
    }

    private string AplicarCssLeitura(string html)
    {
        var css = AparenciaLeituraEpub.GerarCss(_preferencias, CorFundo, ObterCorTexto());
        var fimCabecalho = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        if (fimCabecalho >= 0) return html.Insert(fimCabecalho, css);
        var inicioCorpo = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        if (inicioCorpo >= 0) return html.Insert(inicioCorpo, $"<head>{css}</head>");
        return $"<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />{css}</head><body>{html}</body></html>";
    }

    private async Task AtualizarPreferenciasAsync(Func<PreferenciasLeituraEpub, PreferenciasLeituraEpub> atualizar)
    {
        if (EstaFechando) return;
        _preferencias = atualizar(_preferencias);
        try { AparenciaLeituraEpub.Salvar(Preferences.Default, _preferencias); }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine(exception); }
        AtualizarCoresLeitura();
        NotificarAparenciaAlterada();
        if (!string.IsNullOrWhiteSpace(_htmlCapituloAtual) && !EstaFechando) ConteudoCapitulo = CriarFonteHtml(_htmlCapituloAtual);
        await Task.CompletedTask;
    }

    private async Task SalvarProgressoAsync()
    {
        if (Item is null || Capitulos.Count == 0) return;
        await _bloqueioProgresso.WaitAsync();
        try
        {
            Item.CurrentPage = CapituloAtual;
            Item.TotalPages = Capitulos.Count;
            Item.LastReadAt = DateTime.Now;
            await _database.SalvarObraBibliotecaAsync(Item);
        }
        finally { _bloqueioProgresso.Release(); }
    }

    [RelayCommand] private async Task AvancarCapituloAsync()
    {
        if (!PodeAvancarCapitulo || EstaFechando) return;
        CapituloAtual++;
        await CarregarCapituloAtualAsync();
        RegistrarInteracao();
    }

    [RelayCommand] private async Task VoltarCapituloAsync()
    {
        if (!PodeVoltarCapitulo || EstaFechando) return;
        CapituloAtual--;
        await CarregarCapituloAtualAsync();
        RegistrarInteracao();
    }

    [RelayCommand] private void AlternarControles()
    {
        if (EstaFechando) return;
        _estadoFoco.AlternarControles();
        SincronizarModoFoco();
        if (ControlesVisiveis) AgendarOcultacaoAutomatica(); else CancelarOcultacaoAutomatica();
    }

    [RelayCommand] private void AbrirAparencia()
    {
        if (EstaFechando) return;
        CancelarOcultacaoAutomatica();
        _estadoFoco.AbrirConfiguracoes();
        SincronizarModoFoco();
    }

    [RelayCommand] private void FecharAparencia()
    {
        _estadoFoco.FecharConfiguracoes();
        SincronizarModoFoco();
        AgendarOcultacaoAutomatica();
    }

    [RelayCommand] private async Task VoltarAsync()
    {
        if (Interlocked.Exchange(ref _navegacaoIniciada, 1) != 0) return;
        await FecharAsync();
        await Shell.Current.GoToAsync("..");
    }

    partial void OnCapituloAtualChanged(int value) => AtualizarEstadoCapitulo();
    partial void OnEstaCarregandoChanged(bool value) => AtualizarEstadoCapitulo();

    private void AtualizarEstadoCapitulo()
    {
        TextoCapitulo = Capitulos.Count == 0 ? string.Empty : $"{CapituloAtual + 1} / {Capitulos.Count}";
        OnPropertyChanged(nameof(PodeVoltarCapitulo));
        OnPropertyChanged(nameof(PodeAvancarCapitulo));
        OnPropertyChanged(nameof(ProgressoCapitulo));
    }

    private void SincronizarModoFoco()
    {
        ControlesVisiveis = _estadoFoco.ControlesVisiveis;
        PainelAparenciaVisivel = _estadoFoco.ConfiguracoesVisiveis;
        OnPropertyChanged(nameof(ControlesOcultos));
    }

    private void AgendarOcultacaoAutomatica()
    {
        CancelarOcultacaoAutomatica();
        if (EstaFechando || PainelAparenciaVisivel || !ControlesVisiveis) return;
        _cancelamentoOcultacao = new CancellationTokenSource();
        _ = OcultarControlesAutomaticamenteAsync(_cancelamentoOcultacao.Token);
    }

    private async Task OcultarControlesAutomaticamenteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AtrasoModoFoco, cancellationToken);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (EstaFechando || PainelAparenciaVisivel) return;
                _estadoFoco.OcultarControles();
                SincronizarModoFoco();
            });
        }
        catch (OperationCanceledException) { }
    }

    private void CancelarOperacoes()
    {
        _cancelamentoCarregamento?.Cancel();
        _cancelamentoOcultacao?.Cancel();
    }

    private void CancelarOcultacaoAutomatica()
    {
        _cancelamentoOcultacao?.Cancel();
        _cancelamentoOcultacao?.Dispose();
        _cancelamentoOcultacao = null;
    }

    private PreferenciasLeituraEpub CarregarPreferencias()
    {
        try { return AparenciaLeituraEpub.Carregar(Preferences.Default); }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            return new PreferenciasLeituraEpub(TemaLeituraEpub.Claro, FonteLeituraEpub.Sistema, AparenciaLeituraEpub.TamanhoTextoPadrao, AparenciaLeituraEpub.EspacamentoLinhasPadrao, AparenciaLeituraEpub.MargemPadrao, AlinhamentoLeituraEpub.Justificado);
        }
    }

    private void AtualizarCoresLeitura()
    {
        CorFundo = ObterRecursoCor(_preferencias.Tema switch
        {
            TemaLeituraEpub.Escuro => "EpubDarkBackground",
            TemaLeituraEpub.Sepia => "EpubSepiaBackground",
            _ => "EpubLightBackground"
        });
    }

    private Color ObterCorTexto() => ObterRecursoCor(_preferencias.Tema switch
    {
        TemaLeituraEpub.Escuro => "EpubDarkText",
        TemaLeituraEpub.Sepia => "EpubSepiaText",
        _ => "EpubLightText"
    });

    private static Color ObterRecursoCor(string chave) => Application.Current?.Resources.TryGetValue(chave, out var valor) == true && valor is Color cor ? cor : Colors.Black;

    private void NotificarAparenciaAlterada()
    {
        foreach (var nome in new[] { nameof(TemaClaroSelecionado), nameof(TemaEscuroSelecionado), nameof(TemaSepiaSelecionado), nameof(FonteSistemaSelecionada), nameof(FonteSansSerifSelecionada), nameof(FonteSerifSelecionada), nameof(AlinhamentoEsquerdaSelecionado), nameof(AlinhamentoJustificadoSelecionado), nameof(TamanhoTexto), nameof(EspacamentoLinhas), nameof(MargemLeitura), nameof(TextoTamanhoTexto), nameof(TextoEspacamentoLinhas), nameof(TextoMargemLeitura) }) OnPropertyChanged(nome);
    }

    private bool EstaFechando => Volatile.Read(ref _estaFechando) != 0;

    private static TEnum InterpretarEnum<TEnum>(string? valor, TEnum valorAtual) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(valor, true, out var interpretado) && Enum.IsDefined(interpretado) ? interpretado : valorAtual;
}
