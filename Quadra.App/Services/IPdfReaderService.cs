using Quadra.App.Models;

namespace Quadra.App.Services;

public interface IPdfReaderService
{
    Task<List<ComicPage>> LoadPagesAsync(
        LibraryItem item,
        CancellationToken cancellationToken = default);

    void ClearCache(LibraryItem item);
}