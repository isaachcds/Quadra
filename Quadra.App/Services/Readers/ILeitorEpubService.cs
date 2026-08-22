using Quadra.App.Models;

namespace Quadra.App.Services.Readers;

public interface ILeitorEpubService
{
    Task<List<EpubChapter>> CarregarCapitulosAsync(
        ObraBiblioteca item,
        CancellationToken cancellationToken = default);

    void LimparCache(ObraBiblioteca item);

    string ObterRaizConteudo(ObraBiblioteca item);
}
