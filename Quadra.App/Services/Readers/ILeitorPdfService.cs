using Quadra.App.Models;

namespace Quadra.App.Services.Readers;

public interface ILeitorPdfService
{
    Task<List<ComicPage>> CarregarPaginasAsync(
        ObraBiblioteca item,
        CancellationToken cancellationToken = default);

    void LimparCache(ObraBiblioteca item);
}
