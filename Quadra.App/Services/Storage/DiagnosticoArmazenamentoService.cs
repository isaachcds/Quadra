using Quadra.App.Data;

namespace Quadra.App.Services.Storage;

public sealed record ResumoArmazenamento(long BytesBiblioteca, long BytesCache, long? BytesDisponiveis);

public sealed class DiagnosticoArmazenamentoService
{
    private static readonly string[] DiretoriosCacheRegeneraveis = ["Comics", "EpubBooks", "PdfPages"];
    private readonly QuadraDatabase _database;
    private readonly IEspacoArmazenamentoService _espacoArmazenamentoService;

    public DiagnosticoArmazenamentoService(QuadraDatabase database, IEspacoArmazenamentoService espacoArmazenamentoService)
    {
        _database = database;
        _espacoArmazenamentoService = espacoArmazenamentoService;
    }

    public async Task<ResumoArmazenamento> ObterResumoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var obras = await _database.ObterObrasBibliotecaAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var arquivosBiblioteca = obras.SelectMany(obra => new[] { obra.FilePath, obra.CoverPath })
            .Where(path => !string.IsNullOrWhiteSpace(path)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase);
        var bytesBiblioteca = arquivosBiblioteca.Sum(ObterTamanhoArquivo);
        var bytesCache = DiretoriosCacheRegeneraveis.Sum(diretorio =>
            ObterTamanhoDiretorio(Path.Combine(FileSystem.Current.CacheDirectory, diretorio)));
        var espaco = _espacoArmazenamentoService.ObterEspacoDisponivel(FileSystem.Current.AppDataDirectory);

        return new ResumoArmazenamento(bytesBiblioteca, bytesCache, espaco.BytesDisponiveis);
    }

    public Task LimparCachesAsync(CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        foreach (var diretorio in DiretoriosCacheRegeneraveis)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var caminho = Path.Combine(FileSystem.Current.CacheDirectory, diretorio);
            if (Directory.Exists(caminho))
                Directory.Delete(caminho, recursive: true);
        }
    }, cancellationToken);

    private static long ObterTamanhoArquivo(string caminho)
    {
        try { return File.Exists(caminho) ? Math.Max(0, new FileInfo(caminho).Length) : 0; }
        catch (IOException) { return 0; }
        catch (UnauthorizedAccessException) { return 0; }
    }

    private static long ObterTamanhoDiretorio(string caminho)
    {
        try { return Directory.Exists(caminho) ? Directory.EnumerateFiles(caminho, "*", SearchOption.AllDirectories).Sum(ObterTamanhoArquivo) : 0; }
        catch (IOException) { return 0; }
        catch (UnauthorizedAccessException) { return 0; }
    }
}
