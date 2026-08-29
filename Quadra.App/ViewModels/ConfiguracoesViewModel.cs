using CommunityToolkit.Mvvm.ComponentModel;
using Quadra.App.Presentation;
using Quadra.App.Services.Storage;
using System.Globalization;

namespace Quadra.App.ViewModels;

public partial class ConfiguracoesViewModel : ObservableObject
{
    private readonly DiagnosticoArmazenamentoService _diagnosticoArmazenamentoService;

    [ObservableProperty] public partial TemaAplicativo TemaSelecionado { get; set; }
    [ObservableProperty] public partial int IndiceOrdenacaoBiblioteca { get; set; }
    [ObservableProperty] public partial bool NavegacaoPorToqueAtivada { get; set; }
    [ObservableProperty] public partial string TextoBiblioteca { get; set; } = string.Empty;
    [ObservableProperty] public partial string TextoCache { get; set; } = string.Empty;
    [ObservableProperty] public partial string TextoEspacoDisponivel { get; set; } = string.Empty;
    [ObservableProperty] public partial string ResumoAparenciaEpub { get; set; } = string.Empty;
    [ObservableProperty] public partial bool EstaAtualizandoArmazenamento { get; set; }

    public bool TemaSistemaSelecionado => TemaSelecionado == TemaAplicativo.Sistema;
    public bool TemaClaroSelecionado => TemaSelecionado == TemaAplicativo.Claro;
    public bool TemaEscuroSelecionado => TemaSelecionado == TemaAplicativo.Escuro;
    public bool PodeLimparCache => !EstaAtualizandoArmazenamento;

    public ConfiguracoesViewModel(DiagnosticoArmazenamentoService diagnosticoArmazenamentoService)
    {
        _diagnosticoArmazenamentoService = diagnosticoArmazenamentoService;
        TemaSelecionado = PreferenciasAplicativo.ObterTema(Preferences.Default);
        IndiceOrdenacaoBiblioteca = Math.Clamp(Preferences.Default.Get(PreferenciasAplicativo.ChaveOrdenacaoBiblioteca, 0), 0, 5);
        NavegacaoPorToqueAtivada = Preferences.Default.Get(PreferenciasAplicativo.ChaveNavegacaoPorToque, true);
        AtualizarResumoEpub();
    }

    public async Task CarregarArmazenamentoAsync()
    {
        if (EstaAtualizandoArmazenamento) return;
        EstaAtualizandoArmazenamento = true;
        try { await AtualizarResumoArmazenamentoAsync(); }
        finally { EstaAtualizandoArmazenamento = false; }
    }

    public async Task LimparCacheAsync()
    {
        if (EstaAtualizandoArmazenamento) return;
        EstaAtualizandoArmazenamento = true;
        try
        {
            await _diagnosticoArmazenamentoService.LimparCachesAsync();
            await AtualizarResumoArmazenamentoAsync();
        }
        finally { EstaAtualizandoArmazenamento = false; }
    }

    partial void OnTemaSelecionadoChanged(TemaAplicativo value)
    {
        Preferences.Default.Set(PreferenciasAplicativo.ChaveTemaAplicativo, (int)value);
        if (Application.Current is { } application) PreferenciasAplicativo.AplicarTema(application, value);
        OnPropertyChanged(nameof(TemaSistemaSelecionado));
        OnPropertyChanged(nameof(TemaClaroSelecionado));
        OnPropertyChanged(nameof(TemaEscuroSelecionado));
    }

    partial void OnEstaAtualizandoArmazenamentoChanged(bool value) => OnPropertyChanged(nameof(PodeLimparCache));

    partial void OnIndiceOrdenacaoBibliotecaChanged(int value) => Preferences.Default.Set(PreferenciasAplicativo.ChaveOrdenacaoBiblioteca, Math.Clamp(value, 0, 5));
    partial void OnNavegacaoPorToqueAtivadaChanged(bool value) => Preferences.Default.Set(PreferenciasAplicativo.ChaveNavegacaoPorToque, value);

    private async Task AtualizarResumoArmazenamentoAsync()
    {
        var resumo = await _diagnosticoArmazenamentoService.ObterResumoAsync();
        TextoBiblioteca = FormatarTamanho(resumo.BytesBiblioteca);
        TextoCache = FormatarTamanho(resumo.BytesCache);
        TextoEspacoDisponivel = resumo.BytesDisponiveis is { } bytes ? FormatarTamanho(bytes) : "—";
    }

    private void AtualizarResumoEpub()
    {
        var preferencias = AparenciaLeituraEpub.Carregar(Preferences.Default);
        ResumoAparenciaEpub = $"{preferencias.Tema} · {preferencias.Fonte} · {preferencias.TamanhoTexto:0}px";
    }

    private static string FormatarTamanho(long bytes)
    {
        string[] unidades = ["B", "KB", "MB", "GB", "TB"];
        double tamanho = Math.Max(0, bytes);
        var indice = 0;
        while (tamanho >= 1024 && indice < unidades.Length - 1) { tamanho /= 1024; indice++; }
        return $"{tamanho.ToString(indice == 0 ? "0" : "0.0", CultureInfo.CurrentCulture)} {unidades[indice]}";
    }
}
