using Quadra.App.Models;
using System.IO.Compression;
using VersOne.Epub;

namespace Quadra.App.Services;

public class EpubReaderService : IEpubReaderService
{
    private readonly string _epubCacheDirectory;

    public EpubReaderService()
    {
        _epubCacheDirectory = Path.Combine(
            FileSystem.Current.CacheDirectory,
            "EpubBooks");

        Directory.CreateDirectory(
            _epubCacheDirectory);
    }

    public async Task<List<EpubChapter>> LoadChaptersAsync(
        LibraryItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!File.Exists(item.FilePath))
        {
            throw new FileNotFoundException(
                "O arquivo EPUB não foi encontrado.",
                item.FilePath);
        }

        var itemCacheDirectory =
            GetItemCacheDirectory(item);

        await ExtractBookAsync(
            item.FilePath,
            itemCacheDirectory,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var book = await EpubReader.ReadBookAsync(
            item.FilePath);

        var chapters = new List<EpubChapter>();

        for (var index = 0;
             index < book.ReadingOrder.Count;
             index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contentFile =
                book.ReadingOrder[index];

            var relativePath =
                NormalizeRelativePath(
                    contentFile.FilePath);

            var localFilePath =
                Path.Combine(
                    itemCacheDirectory,
                    relativePath);

            if (!File.Exists(localFilePath))
                continue;

            chapters.Add(new EpubChapter
            {
                Index = chapters.Count,
                Title = $"Capítulo {chapters.Count + 1}",
                OriginalPath = contentFile.FilePath,
                LocalFilePath = localFilePath
            });
        }

        if (chapters.Count == 0)
        {
            throw new InvalidOperationException(
                "Nenhum capítulo compatível foi encontrado no EPUB.");
        }

        return chapters;
    }

    public void ClearCache(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var directory =
            GetItemCacheDirectory(item);

        if (Directory.Exists(directory))
        {
            Directory.Delete(
                directory,
                recursive: true);
        }
    }

    private static async Task ExtractBookAsync(
        string epubFilePath,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var completedMarker = Path.Combine(
            destinationDirectory,
            ".extraction-complete");

        if (File.Exists(completedMarker))
            return;

        if (Directory.Exists(destinationDirectory))
        {
            Directory.Delete(
                destinationDirectory,
                recursive: true);
        }

        Directory.CreateDirectory(
            destinationDirectory);

        await Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                ZipFile.ExtractToDirectory(
                    epubFilePath,
                    destinationDirectory,
                    overwriteFiles: true);
            },
            cancellationToken);

        await File.WriteAllTextAsync(
            completedMarker,
            DateTime.UtcNow.ToString("O"),
            cancellationToken);
    }

    private string GetItemCacheDirectory(
        LibraryItem item)
    {
        var identifier = item.Id > 0
            ? item.Id.ToString()
            : Path.GetFileNameWithoutExtension(
                item.StoredFileName);

        return Path.Combine(
            _epubCacheDirectory,
            identifier);
    }

    private static string NormalizeRelativePath(
        string path)
    {
        var decodedPath =
            Uri.UnescapeDataString(path);

        return decodedPath
            .Replace(
                '/',
                Path.DirectorySeparatorChar)
            .Replace(
                '\\',
                Path.DirectorySeparatorChar)
            .TrimStart(
                Path.DirectorySeparatorChar);
    }
}