using Quadra.App.Models;

namespace Quadra.App.Services.Readers;

//isso aqui é em "teoria" uma solução para compilar para windows, iOS e Mac. Algo como uma implementação compartilhada temporaria

public class LeitorPdfNaoSuportadoService
    : ILeitorPdfService
{
    public Task<List<ComicPage>> CarregarPaginasAsync(
        ObraBiblioteca item,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "O leitor de PDF está disponível apenas no Android nesta versão.");
    }

    public void LimparCache(ObraBiblioteca item)
    {
        // Não existe cache de PDF nesta plataforma.
    }
}
