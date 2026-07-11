using Quadra.App.Models;

namespace Quadra.App.Services;

public interface IEpubReaderService
{
    Task<List<EpubChapter>> LoadChaptersAsync(
        LibraryItem item,
        CancellationToken cancellationToken = default);

    void ClearCache(LibraryItem item);
}