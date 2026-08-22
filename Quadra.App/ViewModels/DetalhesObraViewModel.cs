using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Pages;
using Quadra.App.Presentation;
using Quadra.App.Services.Readers;
using Quadra.App.Services.Storage;

namespace Quadra.App.ViewModels;

public partial class DetalhesObraViewModel : ObservableObject, IQueryAttributable
{
    private readonly QuadraDatabase _database;
    private readonly LimpezaBibliotecaService _cleanupService;
    private readonly LeitorQuadrinhosService _leitorQuadrinhosService;
    private CancellationTokenSource? _preparationCancellation;

    [ObservableProperty]
    public partial ObraBiblioteca? Item { get; set; }

    [ObservableProperty]
    public partial string TextoFormato { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TextoTotal { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TextoEstadoProgresso { get; set; } = "Não iniciado";

    [ObservableProperty]
    public partial string TextoPosicaoProgresso { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TextoPercentualProgresso { get; set; } = "0%";

    [ObservableProperty]
    public partial double PercentualProgresso { get; set; }

    [ObservableProperty]
    public partial string TextoBotaoLeitura { get; set; } = "Começar leitura";

    [ObservableProperty]
    public partial string TextoDataImportacao { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TextoUltimaLeitura { get; set; } = "Nunca";

    [ObservableProperty]
    public partial string TextoTamanhoArquivo { get; set; } = "Indisponível";

    [ObservableProperty]
    public partial bool PossuiTamanhoArquivo { get; set; }

    [ObservableProperty]
    public partial bool ArquivoExiste { get; set; }

    [ObservableProperty]
    public partial bool PossuiCapa { get; set; }

    [ObservableProperty]
    public partial bool EstaCarregando { get; set; }

    [ObservableProperty]
    public partial bool TemErroCarregamento { get; set; }

    [ObservableProperty]
    public partial string MensagemErro { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool EstaPreparandoLeitura { get; set; }

    [ObservableProperty]
    public partial string TextoPreparacao { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool EstaExcluindo { get; set; }

    [ObservableProperty]
    public partial bool ConteudoValido { get; set; }

    [ObservableProperty]
    public partial bool ArquivoAusente { get; set; }

    [ObservableProperty]
    public partial bool PodeLer { get; set; }

    [ObservableProperty]
    public partial bool PodeExcluir { get; set; }

    [ObservableProperty]
    public partial string DescricaoCapa { get; set; } = "Capa da obra";

    [ObservableProperty]
    public partial string DescricaoProgresso { get; set; } = "Leitura não iniciada";

    [ObservableProperty]
    public partial string DescricaoBotaoLeitura { get; set; } = "Começar leitura";

    public DetalhesObraViewModel(
        QuadraDatabase database,
        LimpezaBibliotecaService cleanupService,
        LeitorQuadrinhosService leitorQuadrinhosService)
    {
        _database = database;
        _cleanupService = cleanupService;
        _leitorQuadrinhosService = leitorQuadrinhosService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("Item", out var value) ||
            value is not ObraBiblioteca libraryItem)
        {
            TemErroCarregamento = true;
            MensagemErro = "Não foi possível carregar os detalhes desta obra.";
            AtualizarEstados();
            return;
        }

        Item = libraryItem;
        EstaCarregando = true;
        TemErroCarregamento = false;
        AtualizarApresentacao();
        AtualizarEstados();
    }

    [RelayCommand]
    private async Task IniciarLeituraAsync()
    {
        if (Item is null || !PodeLer)
            return;

        var paginasCarregadas = 0;
        var etapa = "início";

        var leituraConcluida =
            Item.TotalPages > 0 &&
            Item.CurrentPage >= Item.TotalPages - 1;

        if (leituraConcluida)
            Item.CurrentPage = 0;

        try
        {
            EstaPreparandoLeitura = true;
            TextoPreparacao = Item.Format.Equals(
                "EPUB",
                StringComparison.OrdinalIgnoreCase)
                ? "Abrindo livro…"
                : "Preparando páginas…";
            AtualizarEstados();

            if (Item.Format.Equals("EPUB", StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.GoToAsync(
                    "EpubReaderPage",
                    new Dictionary<string, object> { ["Item"] = Item });
                return;
            }

            _preparationCancellation?.Dispose();
            _preparationCancellation = new CancellationTokenSource();

            etapa = "preparação do formato";
            RegistrarPreparacaoLeitor(etapa, Item, _preparationCancellation.Token, paginasCarregadas);

            var paginas = await _leitorQuadrinhosService.CarregarPaginasAsync(
                Item,
                _preparationCancellation.Token);
            paginasCarregadas = paginas.Count;
            RegistrarPreparacaoLeitor("páginas preparadas", Item, _preparationCancellation.Token, paginasCarregadas);

            if (paginas.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Nenhuma página encontrada",
                    "O arquivo não possui imagens compatíveis.",
                    "OK");
                return;
            }

            Item.TotalPages = paginas.Count;

            if (Item.CurrentPage < 0 || Item.CurrentPage >= Item.TotalPages)
                Item.CurrentPage = 0;

            etapa = "gravação do total e posição";
            await _database.SalvarObraBibliotecaAsync(Item);
            AtualizarApresentacao();

            etapa = "navegação para LeitorPage";
            RegistrarPreparacaoLeitor(etapa, Item, _preparationCancellation.Token, paginasCarregadas);
            await Shell.Current.GoToAsync(
                "ReaderPage",
                new Dictionary<string, object> { ["Item"] = Item });
        }
        catch (OperationCanceledException)
        {
            // A tela deixou de precisar desta preparação.
        }
        catch (Exception exception)
        {
            RegistrarFalhaPreparacaoLeitor(
                etapa,
                exception,
                Item,
                _preparationCancellation?.Token,
                paginasCarregadas);
            await Shell.Current.DisplayAlertAsync(
                "Erro ao preparar leitura",
                "Não foi possível preparar este arquivo para leitura.",
                "OK");
        }
        finally
        {
            EstaPreparandoLeitura = false;
            TextoPreparacao = string.Empty;
            AtualizarEstados();
        }
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (Item is null || !PodeExcluir)
            return;

        var confirmou = await Shell.Current.DisplayAlertAsync(
            "Excluir obra",
            $"Deseja excluir \"{Item.Title}\"? A cópia interna e os dados de leitura serão removidos. O arquivo original externo não será apagado.",
            "Excluir",
            "Cancelar");

        if (!confirmou)
            return;

        try
        {
            EstaExcluindo = true;
            AtualizarEstados();
            await _cleanupService.ExcluirAsync(Item);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            await Shell.Current.DisplayAlertAsync(
                "Erro ao excluir",
                "Não foi possível excluir esta obra. Tente novamente.",
                "OK");
        }
        finally
        {
            EstaExcluindo = false;
            AtualizarEstados();
        }
    }

    [RelayCommand]
    private async Task AtualizarDetalhesAsync()
    {
        if (Item is null)
        {
            TemErroCarregamento = true;
            MensagemErro = "Não foi possível carregar os detalhes desta obra.";
            AtualizarEstados();
            return;
        }

        try
        {
            EstaCarregando = true;
            TemErroCarregamento = false;
            MensagemErro = string.Empty;
            AtualizarEstados();

            if (Item.Id > 0)
            {
                var itemAtualizado = await _database.ObterObraBibliotecaAsync(Item.Id);
                if (itemAtualizado is null)
                {
                    TemErroCarregamento = true;
                    MensagemErro = "Esta obra não está mais disponível na biblioteca.";
                    return;
                }

                Item = itemAtualizado;
            }

            var fileState = await InspectFilesAsync(Item);
            ArquivoExiste = fileState.FileExists;
            PossuiCapa = fileState.CapaExiste;
            PossuiTamanhoArquivo = fileState.FileSizeBytes.HasValue;
            TextoTamanhoArquivo = ApresentacaoDetalhesObra.FormatarTamanhoArquivo(
                fileState.FileSizeBytes);
            AtualizarApresentacao();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            TemErroCarregamento = true;
            MensagemErro = "Não foi possível carregar os detalhes desta obra.";
        }
        finally
        {
            EstaCarregando = false;
            AtualizarEstados();
        }
    }

    public void CancelarPreparacao()
    {
        _preparationCancellation?.Cancel();
    }

    private static void RegistrarPreparacaoLeitor(
        string etapa,
        ObraBiblioteca item,
        CancellationToken cancellationToken,
        int paginasCarregadas)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[AberturaLeitor] Etapa={etapa}; Formato={item.Format}; " +
            $"Caminho={item.FilePath}; Cancelado={cancellationToken.IsCancellationRequested}; " +
            $"Paginas={paginasCarregadas}; PosicaoInicial={item.CurrentPage}");
    }

    private static void RegistrarFalhaPreparacaoLeitor(
        string etapa,
        Exception exception,
        ObraBiblioteca item,
        CancellationToken? cancellationToken,
        int paginasCarregadas)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[AberturaLeitor] Falha na etapa={etapa}; Tipo={exception.GetType().FullName}; " +
            $"Mensagem={exception.Message}; Formato={item.Format}; Caminho={item.FilePath}; " +
            $"Cancelado={cancellationToken?.IsCancellationRequested}; Paginas={paginasCarregadas}; " +
            $"PosicaoInicial={item.CurrentPage}; InnerException={exception.InnerException}; " +
            $"StackTrace={exception.StackTrace}");
    }

    private void AtualizarApresentacao()
    {
        if (Item is null)
            return;

        TextoFormato = Item.Format.ToUpperInvariant();
        TextoTotal = ApresentacaoDetalhesObra.FormatarTotal(
            Item.Format,
            Item.TotalPages);
        TextoDataImportacao = Item.ImportedAt == default
            ? "Indisponível"
            : Item.ImportedAt.ToString("dd/MM/yyyy");
        TextoUltimaLeitura = Item.LastReadAt.HasValue
            ? Item.LastReadAt.Value.ToString("dd/MM/yyyy 'às' HH:mm")
            : "Nunca";

        var progress = ApresentacaoDetalhesObra.CalcularProgresso(
            Item.Format,
            Item.CurrentPage,
            Item.TotalPages,
            Item.LastReadAt.HasValue);

        PercentualProgresso = progress.Percentual;
        TextoEstadoProgresso = progress.TextoStatus;
        TextoPosicaoProgresso = progress.TextoPosicao;
        TextoPercentualProgresso = $"{progress.Percentual * 100:0}%";
        TextoBotaoLeitura = progress.TextoBotao;
        DescricaoCapa = $"Capa de {Item.Title}, formato {TextoFormato}";
        DescricaoProgresso = $"{progress.TextoStatus}. {TextoPercentualProgresso}. {progress.TextoPosicao}.";
        DescricaoBotaoLeitura = $"{progress.TextoBotao}: {Item.Title}";
    }

    private void AtualizarEstados()
    {
        var hasItem = Item is not null;
        ConteudoValido = hasItem &&
                         !EstaCarregando &&
                         !TemErroCarregamento &&
                         ArquivoExiste;
        ArquivoAusente = hasItem &&
                         !EstaCarregando &&
                         !TemErroCarregamento &&
                         !ArquivoExiste;
        PodeLer = ConteudoValido &&
                  !EstaPreparandoLeitura &&
                  !EstaExcluindo;
        PodeExcluir = hasItem &&
                      !EstaCarregando &&
                      !EstaPreparandoLeitura &&
                      !EstaExcluindo;
    }

    private static Task<FileState> InspectFilesAsync(ObraBiblioteca item)
    {
        return Task.Run(() =>
        {
            var fileExists = ApresentacaoDetalhesObra.ArquivoDisponivel(item.FilePath);
            var coverExists = ApresentacaoDetalhesObra.ArquivoDisponivel(item.CoverPath);
            long? size = null;

            if (fileExists)
            {
                try
                {
                    size = new FileInfo(item.FilePath).Length;
                }
                catch (Exception exception) when (
                    exception is IOException or
                    UnauthorizedAccessException or
                    NotSupportedException)
                {
                    System.Diagnostics.Debug.WriteLine(exception);
                }
            }

            return new FileState(fileExists, coverExists, size);
        });
    }

    private sealed record FileState(
        bool FileExists,
        bool CapaExiste,
        long? FileSizeBytes);
}
