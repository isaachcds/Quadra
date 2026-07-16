using System.IO.Compression;
using Quadra.App.Models;
using System.Xml.Linq;
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
    private readonly IStorageSpaceService _storageSpaceService;

    private readonly string _coversDirectory;

    public CoverService(
        IStorageSpaceService storageSpaceService,
        IPdfCoverService? pdfCoverService = null)
    {
        _storageSpaceService = storageSpaceService;
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

            "EPUB" => await GenerateEpubCoverAsync(
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

        EnsureCoverSpace(coverPath, StorageSpacePolicy.CoverAllowanceBytes);

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
            Path.GetExtension(firstImage.Key!));

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

    private async Task<string?> GenerateEpubCoverAsync(
    LibraryItem item,
    CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(item.FilePath);

        var packagePath = FindEpubPackagePath(archive);

        ZipArchiveEntry? coverEntry = null;

        if (!string.IsNullOrWhiteSpace(packagePath))
        {
            coverEntry = await FindEpubCoverFromPackageAsync(
                archive,
                packagePath,
                cancellationToken);
        }

        // Fallback para EPUBs que não identificam corretamente a capa.
        coverEntry ??= archive.Entries
            .Where(entry =>
                !string.IsNullOrWhiteSpace(entry.Name) &&
                IsImage(entry.FullName))
            .OrderByDescending(entry =>
                ContainsCoverName(entry.FullName))
            .ThenBy(
                entry => entry.FullName,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (coverEntry is null)
            return null;

        var extension = NormalizeCoverExtension(
            Path.GetExtension(coverEntry.Name));

        var coverPath = CreateCoverPath(
            item,
            extension);

        await using var inputStream =
            coverEntry.Open();

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

    private static string? FindEpubPackagePath(
    ZipArchive archive)
    {
        var containerEntry = archive.GetEntry(
            "META-INF/container.xml");

        if (containerEntry is null)
            return null;

        using var stream = containerEntry.Open();

        var document = XDocument.Load(stream);

        var rootFile = document
            .Descendants()
            .FirstOrDefault(element =>
                element.Name.LocalName.Equals(
                    "rootfile",
                    StringComparison.OrdinalIgnoreCase));

        return rootFile?
            .Attribute("full-path")?
            .Value;
    }

    private static async Task<ZipArchiveEntry?>
    FindEpubCoverFromPackageAsync(
        ZipArchive archive,
        string packagePath,
        CancellationToken cancellationToken)
    {
        var packageEntry = GetArchiveEntry(
            archive,
            packagePath);

        if (packageEntry is null)
            return null;

        XDocument packageDocument;

        await using (var packageStream =
                     packageEntry.Open())
        {
            packageDocument = await XDocument.LoadAsync(
                packageStream,
                LoadOptions.None,
                cancellationToken);
        }

        var manifestItems = packageDocument
            .Descendants()
            .Where(element =>
                element.Name.LocalName.Equals(
                    "item",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        /*
         * EPUB 3:
         * <item properties="cover-image" ... />
         */
        var coverItem = manifestItems
            .FirstOrDefault(element =>
            {
                var properties =
                    element.Attribute("properties")?.Value;

                return ContainsProperty(
                    properties,
                    "cover-image");
            });

        /*
         * EPUB 2:
         * <meta name="cover" content="cover-id" />
         */
        if (coverItem is null)
        {
            var coverId = packageDocument
                .Descendants()
                .FirstOrDefault(element =>
                    element.Name.LocalName.Equals(
                        "meta",
                        StringComparison.OrdinalIgnoreCase) &&
                    element.Attribute("name")?.Value.Equals(
                        "cover",
                        StringComparison.OrdinalIgnoreCase) == true)
                ?.Attribute("content")
                ?.Value;

            if (!string.IsNullOrWhiteSpace(coverId))
            {
                coverItem = manifestItems
                    .FirstOrDefault(element =>
                        element.Attribute("id")?.Value.Equals(
                            coverId,
                            StringComparison.OrdinalIgnoreCase) == true);
            }
        }

        /*
         * Fallback no manifesto:
         * procura imagens com "cover" no id ou caminho.
         */
        coverItem ??= manifestItems
            .Where(IsManifestImage)
            .OrderByDescending(element =>
                ContainsCoverName(
                    element.Attribute("id")?.Value) ||
                ContainsCoverName(
                    element.Attribute("href")?.Value))
            .FirstOrDefault();

        var coverHref =
            coverItem?.Attribute("href")?.Value;

        if (string.IsNullOrWhiteSpace(coverHref))
            return null;

        var decodedHref =
            Uri.UnescapeDataString(coverHref);

        var packageDirectory =
            GetArchiveDirectory(packagePath);

        var fullCoverPath =
            CombineArchivePath(
                packageDirectory,
                decodedHref);

        return GetArchiveEntry(
            archive,
            fullCoverPath);
    }

    private static bool ContainsProperty(
    string? properties,
    string expectedProperty)
    {
        if (string.IsNullOrWhiteSpace(properties))
            return false;

        return properties
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Contains(
                expectedProperty,
                StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsCoverName(
    string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains(
                   "cover",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string CombineArchivePath(
    string baseDirectory,
    string relativePath)
    {
        var combined = string.IsNullOrWhiteSpace(baseDirectory)
            ? relativePath
            : $"{baseDirectory}/{relativePath}";

        var segments = new List<string>();

        foreach (var segment in combined
                     .Replace('\\', '/')
                     .Split(
                         '/',
                         StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;

            if (segment == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);

                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments);
    }

    private static ZipArchiveEntry? GetArchiveEntry(
    ZipArchive archive,
    string path)
    {
        var normalizedPath =
            path.Replace('\\', '/')
                .TrimStart('/');

        return archive.Entries
            .FirstOrDefault(entry =>
                entry.FullName
                    .Replace('\\', '/')
                    .Equals(
                        normalizedPath,
                        StringComparison.OrdinalIgnoreCase));
    }

    private static string GetArchiveDirectory(
    string archivePath)
    {
        var normalizedPath =
            archivePath.Replace('\\', '/');

        var lastSeparator =
            normalizedPath.LastIndexOf('/');

        return lastSeparator < 0
            ? string.Empty
            : normalizedPath[..lastSeparator];
    }

    private static bool IsManifestImage(
    XElement element)
    {
        var mediaType =
            element.Attribute("media-type")?.Value;

        if (!string.IsNullOrWhiteSpace(mediaType) &&
            mediaType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var href =
            element.Attribute("href")?.Value;

        return !string.IsNullOrWhiteSpace(href) &&
               IsImage(href);
    }

    private async Task SaveCoverAsync(
        Stream inputStream,
        string coverPath,
        CancellationToken cancellationToken)
    {
        var estimatedBytes = inputStream.CanSeek
            ? Math.Max(0, inputStream.Length - inputStream.Position)
            : StorageSpacePolicy.CoverAllowanceBytes;

        EnsureCoverSpace(coverPath, estimatedBytes);

        await AtomicFile.WriteAsync(
            coverPath,
            outputStream => inputStream.CopyToAsync(
                outputStream,
                cancellationToken),
            cancellationToken: cancellationToken);
    }

    private void EnsureCoverSpace(string destinationPath, long estimatedBytes)
    {
        StorageSpacePolicy.EnsureAvailable(
            _storageSpaceService,
            destinationPath,
            estimatedBytes,
            "Não há espaço disponível suficiente para gerar a capa.");
    }
}
