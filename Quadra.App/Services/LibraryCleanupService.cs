using Quadra.App.Models;

namespace Quadra.App.Services;

public class LibraryCleanupService
{
    private readonly LibraryStorageService _libraryStorageService;
    private readonly ComicReaderService _comicReaderService;
    private readonly IEpubReaderService _epubReaderService;

    public LibraryCleanupService(
        LibraryStorageService libraryStorageService,
        ComicReaderService comicReaderService,
        IEpubReaderService epubReaderService)
    {
        _libraryStorageService = libraryStorageService;
        _comicReaderService = comicReaderService;
        _epubReaderService = epubReaderService;
    }

    public async Task DeleteFilesAsync(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        DeleteReaderCache(item);

        await _libraryStorageService.DeleteAsync(item);
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