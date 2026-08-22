using Quadra.App.Models;

using Quadra.App.Services.Readers;

namespace Quadra.App.Services.Storage;

public class LimpezaBibliotecaService
{
    private readonly ArmazenamentoBibliotecaService _armazenamentoBibliotecaService;
    private readonly LeitorQuadrinhosService _leitorQuadrinhosService;
    private readonly ILeitorEpubService _leitorEpubService;
    private readonly Data.QuadraDatabase _database;

    public LimpezaBibliotecaService(
        ArmazenamentoBibliotecaService armazenamentoBibliotecaService,
        LeitorQuadrinhosService leitorQuadrinhosService,
        ILeitorEpubService leitorEpubService,
        Data.QuadraDatabase database)
    {
        _armazenamentoBibliotecaService = armazenamentoBibliotecaService;
        _leitorQuadrinhosService = leitorQuadrinhosService;
        _leitorEpubService = leitorEpubService;
        _database = database;
    }

    // Ordem: caches, arquivos persistentes e registro por último. Se uma etapa
    // inesperada falhar, o registro permanece para que a exclusão possa ser repetida.
    public async Task ExcluirAsync(ObraBiblioteca item)
    {
        ArgumentNullException.ThrowIfNull(item);

        ExcluirCacheLeitor(item);

        await _armazenamentoBibliotecaService.ExcluirAsync(item);
        await _database.ExcluirObraBibliotecaAsync(item);
    }

    private void ExcluirCacheLeitor(ObraBiblioteca item)
    {
        try
        {
            if (item.Format.Equals(
                "EPUB",
                StringComparison.OrdinalIgnoreCase))
            {
                _leitorEpubService.LimparCache(item);
                return;
            }

            if (item.Format.Equals(
                    "CBR",
                    StringComparison.OrdinalIgnoreCase) ||
                item.Format.Equals(
                    "CBZ",
                    StringComparison.OrdinalIgnoreCase) ||
                item.Format.Equals(
                    "PDF",
                    StringComparison.OrdinalIgnoreCase))
            {
                _leitorQuadrinhosService.LimparCache(item);
            }
        }
        catch (DirectoryNotFoundException)
        {
            // O cache já não existe.
        }
        catch (FileNotFoundException)
        {
            // O cache já não existe.
        }
    }
}
