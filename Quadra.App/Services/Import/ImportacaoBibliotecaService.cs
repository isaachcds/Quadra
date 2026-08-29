using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Services.Covers;
using Quadra.App.Services.Storage;

namespace Quadra.App.Services.Import;

public sealed class ImportacaoBibliotecaService
{
    private readonly ArmazenamentoBibliotecaService _armazenamento;
    private readonly CapaService _capas;
    private readonly QuadraDatabase _database;

    public ImportacaoBibliotecaService(ArmazenamentoBibliotecaService armazenamento, CapaService capas, QuadraDatabase database)
    { _armazenamento=armazenamento; _capas=capas; _database=database; }

    public async Task<ObraBiblioteca> ImportarAsync(FileResult arquivo, CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(arquivo);
        var extensao=Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if(!SupportedFileFormats.IsSupported(extensao)) throw new InvalidOperationException("Escolha um arquivo CBR, CBZ, PDF ou EPUB.");
        ObraBiblioteca? obra=null;
        try { obra=await _armazenamento.ImportarAsync(arquivo,cancellationToken);obra.CoverPath=await _capas.GerarCapaAsync(obra,cancellationToken);await _database.SalvarObraBibliotecaAsync(obra);return obra; }
        catch { if(obra is { Id:0 }) await _armazenamento.ExcluirAsync(obra); throw; }
    }

    public async Task<ObraBiblioteca> ImportarConteudoAsync(string nome, Func<CancellationToken,Task<Stream>> abrirStream, CancellationToken cancellationToken=default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome); ArgumentNullException.ThrowIfNull(abrirStream);
        var extensao=Path.GetExtension(nome).ToLowerInvariant(); if(!SupportedFileFormats.IsSupported(extensao)) throw new InvalidOperationException("Escolha um arquivo CBR, CBZ, PDF ou EPUB.");
        var temporario=Path.Combine(FileSystem.Current.CacheDirectory,$"import-{Guid.NewGuid():N}{extensao}");
        try { await using var entrada=await abrirStream(cancellationToken); await using(var saida=File.Create(temporario)) await entrada.CopyToAsync(saida,cancellationToken); return await ImportarAsync(new FileResult(temporario, nome),cancellationToken); }
        finally { if(File.Exists(temporario)) File.Delete(temporario); }
    }
}
