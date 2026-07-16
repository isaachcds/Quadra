using Quadra.App.Models;

namespace Quadra.App.Services;

public class LibraryCleanupService
{
    private readonly LibraryStorageService _libraryStorageService;
    private readonly ComicReaderService _comicReaderService;
    private readonly IEpubReaderService _epubReaderService;
    private readonly Data.QuadraDatabase _database;

    public LibraryCleanupService(
        LibraryStorageService libraryStorageService,
        ComicReaderService comicReaderService,
        IEpubReaderService epubReaderService,
        Data.QuadraDatabase database)
    {
        _libraryStorageService = libraryStorageService;
        _comicReaderService = comicReaderService;
        _epubReaderService = epubReaderService;
        _database = database;
    }

    // Ordem: caches, arquivos persistentes e registro por último. Se uma etapa
    // inesperada falhar, o registro permanece para que a exclusão possa ser repetida.
    public async Task DeleteAsync(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        DeleteReaderCache(item);

        await _libraryStorageService.DeleteAsync(item);
        await _database.DeleteLibraryItemAsync(item);
    }

    private void DeleteReaderCache(LibraryItem item)
    {
        try
        {
            if (item.Format.Equals(
                "EPUB",
                StringComparison.OrdinalIgnoreCase))
            {
                _epubReaderService.ClearCache(item);
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
                _comicReaderService.ClearCache(item);
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
