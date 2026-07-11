using System.IO.Compression;
using Quadra.App.Models;
using SharpCompress.Archives;

namespace Quadra.App.Services;

public class CoverService
{
    private static readonly string[] ImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp"
    ];

    private readonly IPdfCoverService? _pdfCoverService;

    private readonly string _coversDirectory;

    public CoverService(
     IPdfCoverService? pdfCoverService = null)
    {
        _pdfCoverService = pdfCoverService;

        _coversDirectory = Path.Combine(
            FileSystem.Current.AppDataDirectory,
            "Covers");

        Directory.CreateDirectory(_coversDirectory);
    }

    public async Task<string?> GenerateCoverAsync(
        LibraryItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!File.Exists(item.FilePath))
            return null;

        return item.Format.ToUpperInvariant() switch
        {
            "CBZ" => await GenerateCbzCoverAsync(
                item,
                cancellationToken),

            "CBR" => await GenerateCbrCoverAsync(
                item,
                cancellationToken),

            "PDF" => await GeneratePdfCoverAsync(
                item,
                cancellationToken),

            _ => null
        };
    }

    private async Task<string?> GeneratePdfCoverAsync(
    LibraryItem item,
    CancellationToken cancellationToken)
    {
        if (_pdfCoverService is null)
            return null;

        var coverPath = CreateCoverPath(
            item,
            ".png");

        return await _pdfCoverService.GenerateCoverAsync(
            item,
            coverPath,
            cancellationToken);
    }

    private async Task<string?> GenerateCbzCoverAsync(
        LibraryItem item,
        CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(item.FilePath);

        var firstImage = archive.Entries
            .Where(entry =>
                !string.IsNullOrWhiteSpace(entry.Name) &&
                IsImage(entry.Name))
            .OrderBy(
                entry => entry.FullName,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (firstImage is null)
            return null;

        var extension = NormalizeCoverExtension(
            Path.GetExtension(firstImage.Name));

        var coverPath = CreateCoverPath(
            item,
            extension);

        await using var inputStream = firstImage.Open();

        await SaveCoverAsync(
            inputStream,
            coverPath,
            cancellationToken);

        return coverPath;
    }

    private async Task<string?> GenerateCbrCoverAsync(
        LibraryItem item,
        CancellationToken cancellationToken)
    {
        using var archive = ArchiveFactory.OpenArchive(item.FilePath);

        var firstImage = archive.Entries
            .Where(entry =>
                !entry.IsDirectory &&
                !string.IsNullOrWhiteSpace(entry.Key) &&
                IsImage(entry.Key))
            .OrderBy(
                entry => entry.Key,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (firstImage is null)
            return null;

        var extension = NormalizeCoverExtension(
            Path.GetExtension(firstImage.Key));

        var coverPath = CreateCoverPath(
            item,
            extension);

        await using var inputStream =
            firstImage.OpenEntryStream();

        await SaveCoverAsync(
            inputStream,
            coverPath,
            cancellationToken);

        return coverPath;
    }

    private static bool IsImage(string fileName)
    {
        var extension = Path
            .GetExtension(fileName)
            .ToLowerInvariant();

        return ImageExtensions.Contains(extension);
    }

    private string CreateCoverPath(
        LibraryItem item,
        string extension)
    {
        var fileNameWithoutExtension =
            Path.GetFileNameWithoutExtension(
                item.StoredFileName);

        var coverFileName =
            $"{fileNameWithoutExtension}{extension}";

        return Path.Combine(
            _coversDirectory,
            coverFileName);
    }

    private static string NormalizeCoverExtension(
        string extension)
    {
        extension = extension.ToLowerInvariant();

        return extension switch
        {
            ".jpeg" => ".jpg",
            _ => extension
        };
    }

    private static async Task SaveCoverAsync(
        Stream inputStream,
        string coverPath,
        CancellationToken cancellationToken)
    {
        await using var outputStream = new FileStream(
            coverPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await inputStream.CopyToAsync(
            outputStream,
            cancellationToken);
    }
}