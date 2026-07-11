using Quadra.App.Models;

namespace Quadra.App.Services;

//isso aqui é em "teoria" uma solução para compilar para windows, iOS e Mac. Algo como uma implementação compartilhada temporaria

public class UnsupportedPdfReaderService
    : IPdfReaderService
{
    public Task<List<ComicPage>> LoadPagesAsync(
        LibraryItem item,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "O leitor de PDF está disponível apenas no Android nesta versão.");
    }

    public void ClearCache(LibraryItem item)
    {
        // Não existe cache de PDF nesta plataforma.
    }
}