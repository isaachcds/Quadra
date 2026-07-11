using Quadra.App.Models;
using SharpCompress.Archives;

namespace Quadra.App.Services;

public class ComicReaderService
{
    private static readonly string[] ImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp"
    ];

    private readonly string _comicCacheDirectory;

    public ComicReaderService()
    {
        _comicCacheDirectory = Path.Combine(
            FileSystem.Current.CacheDirectory,
            "Comics");

        Directory.CreateDirectory(_comicCacheDirectory);
    }

    public async Task<List<ComicPage>> LoadPagesAsync(
        LibraryItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!File.Exists(item.FilePath))
        {
            throw new FileNotFoundException(
                "O arquivo da obra não foi encontrado.",
                item.FilePath);
        }

        return item.Format.ToUpperInvariant() switch
        {
            "CBR" => await LoadCbrPagesAsync(
                item,
                cancellationToken),

            "CBZ" => await LoadCbzPagesAsync(
                item,
                cancellationToken),

            _ => throw new NotSupportedException(
                $"O formato {item.Format} ainda não possui leitor.")
        };
    }

    private async Task<List<ComicPage>> LoadCbrPagesAsync(
        LibraryItem item,
        CancellationToken cancellationToken)
    {
        var itemCacheDirectory = PrepareItemCache(item);

        using var archive =
            ArchiveFactory.OpenArchive(item.FilePath);

        var imageEntries = archive.Entries
            .Where(entry =>
                !entry.IsDirectory &&
                !string.IsNullOrWhiteSpace(entry.Key) &&
                IsImage(entry.Key))
            .OrderBy(
                entry => entry.Key,
                NaturalStringComparer.Instance)
            .ToList();

        var pages = new List<ComicPage>();

        for (var index = 0; index < imageEntries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = imageEntries[index];

            var extension = Path
                .GetExtension(entry.Key)
                .ToLowerInvariant();

            var destinationFileName =
                $"{index:D5}{extension}";

            var destinationPath = Path.Combine(
                itemCacheDirectory,
                destinationFileName);

            if (!File.Exists(destinationPath))
            {
                await using var inputStream =
                    entry.OpenEntryStream();

                await using var outputStream = new FileStream(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await inputStream.CopyToAsync(
                    outputStream,
                    cancellationToken);
            }

            pages.Add(new ComicPage
            {
                Index = index,
                FileName = entry.Key,
                FilePath = destinationPath
            });
        }

        return pages;
    }

    private async Task<List<ComicPage>> LoadCbzPagesAsync(
        LibraryItem item,
        CancellationToken cancellationToken)
    {
        var itemCacheDirectory = PrepareItemCache(item);

        using var archive =
            System.IO.Compression.ZipFile.OpenRead(item.FilePath);

        var imageEntries = archive.Entries
            .Where(entry =>
                !string.IsNullOrWhiteSpace(entry.Name) &&
                IsImage(entry.FullName))
            .OrderBy(
                entry => entry.FullName,
                NaturalStringComparer.Instance)
            .ToList();

        var pages = new List<ComicPage>();

        for (var index = 0; index < imageEntries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = imageEntries[index];

            var extension = Path
                .GetExtension(entry.Name)
                .ToLowerInvariant();

            var destinationFileName =
                $"{index:D5}{extension}";

            var destinationPath = Path.Combine(
                itemCacheDirectory,
                destinationFileName);

            if (!File.Exists(destinationPath))
            {
                await using var inputStream = entry.Open();

                await using var outputStream = new FileStream(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await inputStream.CopyToAsync(
                    outputStream,
                    cancellationToken);
            }

            pages.Add(new ComicPage
            {
                Index = index,
                FileName = entry.FullName,
                FilePath = destinationPath
            });
        }

        return pages;
    }

    public void ClearCache(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var directory = GetItemCacheDirectory(item);

        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private string PrepareItemCache(LibraryItem item)
    {
        var directory = GetItemCacheDirectory(item);

        Directory.CreateDirectory(directory);

        return directory;
    }

    private string GetItemCacheDirectory(
        LibraryItem item)
    {
        var identifier = item.Id > 0
            ? item.Id.ToString()
            : Path.GetFileNameWithoutExtension(
                item.StoredFileName);

        return Path.Combine(
            _comicCacheDirectory,
            identifier);
    }

    private static bool IsImage(string fileName)
    {
        var extension = Path
            .GetExtension(fileName)
            .ToLowerInvariant();

        return ImageExtensions.Contains(extension);
    }
}