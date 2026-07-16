using System.IO.Compression;
using System.Xml.Linq;
using SharpCompress.Archives;

namespace Quadra.App.Services;

public static class FileValidationService
{
    private static readonly string[] ImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".webp", ".bmp"
    ];

    public static async Task ValidateAsync(
        string filePath,
        string extension,
        CancellationToken cancellationToken)
    {
        switch (extension.ToLowerInvariant())
        {
            case ".pdf":
                await ValidatePdfAsync(filePath, cancellationToken);
                break;
            case ".cbz":
                ValidateZipComic(filePath);
                break;
            case ".cbr":
                ValidateRarComic(filePath);
                break;
            case ".epub":
                ValidateEpub(filePath);
                break;
            default:
                throw new InvalidDataException("O formato do arquivo não é suportado.");
        }
    }

    private static async Task ValidatePdfAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var signature = new byte[5];

        await using var stream = File.OpenRead(filePath);
        var read = await stream.ReadAsync(signature, cancellationToken);

        if (read != signature.Length ||
            !signature.SequenceEqual("%PDF-"u8.ToArray()))
        {
            throw new InvalidDataException("O arquivo PDF é inválido ou está corrompido.");
        }
    }

    private static void ValidateZipComic(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        ValidateZipLimits(archive);

        if (!archive.Entries.Any(entry =>
                !string.IsNullOrWhiteSpace(entry.Name) &&
                IsImage(entry.Name)))
        {
            throw new InvalidDataException("O CBZ não possui imagens compatíveis.");
        }
    }

    private static void ValidateRarComic(string filePath)
    {
        using var archive = ArchiveFactory.OpenArchive(filePath);
        var entries = archive.Entries.Where(entry => !entry.IsDirectory).ToList();

        if (entries.Count > FileProcessingLimits.MaximumArchiveEntries)
            throw new InvalidDataException("O arquivo possui entradas demais.");

        var expandedBytes = entries.Sum(entry => Math.Max(0, entry.Size));

        if (expandedBytes > FileProcessingLimits.MaximumExpandedBytes)
            throw new InvalidDataException("O conteúdo expandido do arquivo é muito grande.");

        if (!entries.Any(entry =>
                !string.IsNullOrWhiteSpace(entry.Key) &&
                IsImage(entry.Key)))
        {
            throw new InvalidDataException("O CBR não possui imagens compatíveis.");
        }
    }

    private static void ValidateEpub(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        ValidateZipLimits(archive);

        var container = archive.GetEntry("META-INF/container.xml") ??
                        throw new InvalidDataException("O EPUB não possui container.xml.");

        using var stream = container.Open();
        var document = XDocument.Load(stream, LoadOptions.None);
        var packagePath = document.Descendants()
            .FirstOrDefault(element =>
                element.Name.LocalName.Equals("rootfile", StringComparison.OrdinalIgnoreCase))?
            .Attribute("full-path")?.Value;

        if (string.IsNullOrWhiteSpace(packagePath) ||
            archive.GetEntry(packagePath.Replace('\\', '/')) is null)
        {
            throw new InvalidDataException("O EPUB não possui um package válido.");
        }

        if (!archive.Entries.Any(entry =>
                entry.FullName.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                entry.FullName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException("O EPUB não possui conteúdo de leitura válido.");
        }
    }

    private static void ValidateZipLimits(ZipArchive archive)
    {
        if (archive.Entries.Count > FileProcessingLimits.MaximumArchiveEntries)
            throw new InvalidDataException("O arquivo possui entradas demais.");

        long total = 0;

        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.Length > FileProcessingLimits.MaximumPathLength)
                throw new InvalidDataException("O arquivo possui um caminho excessivamente longo.");

            total = checked(total + entry.Length);

            if (total > FileProcessingLimits.MaximumExpandedBytes)
                throw new InvalidDataException("O conteúdo expandido do arquivo é muito grande.");
        }
    }

    private static bool IsImage(string fileName)
    {
        return ImageExtensions.Contains(
            Path.GetExtension(fileName),
            StringComparer.OrdinalIgnoreCase);
    }
}
