using System.IO.Compression;
using Quadra.App.Models;
using Quadra.App.Infrastructure;
using Quadra.App.Policies;
using Quadra.App.Services.Storage;
using System.Xml.Linq;
using SharpCompress.Archives;

namespace Quadra.App.Services.Covers;

public class CapaService
{
    private static readonly string[] ImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp"
    ];

    private readonly ICapaPdfService? _capaPdfService;
    private readonly IEspacoArmazenamentoService _espacoArmazenamentoService;

    private readonly string _diretorioCapas;

    public CapaService(
        IEspacoArmazenamentoService espacoArmazenamentoService,
        ICapaPdfService? capaPdfService = null)
    {
        _espacoArmazenamentoService = espacoArmazenamentoService;
        _capaPdfService = capaPdfService;

        _diretorioCapas = Path.Combine(
            FileSystem.Current.AppDataDirectory,
            "Covers");

        Directory.CreateDirectory(_diretorioCapas);
    }

    public async Task<string?> GerarCapaAsync(
        ObraBiblioteca item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!File.Exists(item.FilePath))
            return null;

        return item.Format.ToUpperInvariant() switch
        {
            "CBZ" => await GerarCapaCbzAsync(
                item,
                cancellationToken),

            "CBR" => await GerarCapaCbrAsync(
                item,
                cancellationToken),

            "PDF" => await GerarCapaPdfAsync(
                item,
                cancellationToken),

            "EPUB" => await GerarCapaEpubAsync(
               item,
               cancellationToken),

            _ => null
        };
    }

    private async Task<string?> GerarCapaPdfAsync(
    ObraBiblioteca item,
    CancellationToken cancellationToken)
    {
        if (_capaPdfService is null)
            return null;

        var coverPath = CriarCaminhoCapa(
            item,
            ".png");

        GarantirEspacoCapa(coverPath, PoliticaEspacoArmazenamento.ReservaCapaBytes);

        return await _capaPdfService.GerarCapaAsync(
            item,
            coverPath,
            cancellationToken);
    }

    private async Task<string?> GerarCapaCbzAsync(
        ObraBiblioteca item,
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

        var extension = NormalizarExtensaoCapa(
            Path.GetExtension(firstImage.Name));

        var coverPath = CriarCaminhoCapa(
            item,
            extension);

        await using var inputStream = firstImage.Open();

        await SalvarCapaAsync(
            inputStream,
            coverPath,
            cancellationToken);

        return coverPath;
    }

    private async Task<string?> GerarCapaCbrAsync(
        ObraBiblioteca item,
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

        var extension = NormalizarExtensaoCapa(
            Path.GetExtension(firstImage.Key!));

        var coverPath = CriarCaminhoCapa(
            item,
            extension);

        await using var inputStream =
            firstImage.OpenEntryStream();

        await SalvarCapaAsync(
            inputStream,
            coverPath,
            cancellationToken);

        return coverPath;
    }

    private async Task<string?> GerarCapaEpubAsync(
    ObraBiblioteca item,
    CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(item.FilePath);

        var packagePath = FindEpubPackagePath(archive);

        ZipArchiveEntry? coverEntry = null;

        if (!string.IsNullOrWhiteSpace(packagePath))
        {
            coverEntry = await LocalizarCapaEpubNoPacoteAsync(
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
                ContemNomeCapa(entry.FullName))
            .ThenBy(
                entry => entry.FullName,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (coverEntry is null)
            return null;

        var extension = NormalizarExtensaoCapa(
            Path.GetExtension(coverEntry.Name));

        var coverPath = CriarCaminhoCapa(
            item,
            extension);

        await using var inputStream =
            coverEntry.Open();

        await SalvarCapaAsync(
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

    private string CriarCaminhoCapa(
        ObraBiblioteca item,
        string extension)
    {
        var fileNameWithoutExtension =
            Path.GetFileNameWithoutExtension(
                item.StoredFileName);

        var coverFileName =
            $"{fileNameWithoutExtension}{extension}";

        return Path.Combine(
            _diretorioCapas,
            coverFileName);
    }

    private static string NormalizarExtensaoCapa(
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
    LocalizarCapaEpubNoPacoteAsync(
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
                ContemNomeCapa(
                    element.Attribute("id")?.Value) ||
                ContemNomeCapa(
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

        var caminhoCompletoCapa =
            CombineArchivePath(
                packageDirectory,
                decodedHref);

        return GetArchiveEntry(
            archive,
            caminhoCompletoCapa);
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

    private static bool ContemNomeCapa(
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

    private async Task SalvarCapaAsync(
        Stream inputStream,
        string coverPath,
        CancellationToken cancellationToken)
    {
        var estimatedBytes = inputStream.CanSeek
            ? Math.Max(0, inputStream.Length - inputStream.Position)
            : PoliticaEspacoArmazenamento.ReservaCapaBytes;

        GarantirEspacoCapa(coverPath, estimatedBytes);

        await AtomicFile.WriteAsync(
            coverPath,
            outputStream => inputStream.CopyToAsync(
                outputStream,
                cancellationToken),
            cancellationToken: cancellationToken);
    }

    private void GarantirEspacoCapa(string destinationPath, long estimatedBytes)
    {
        PoliticaEspacoArmazenamento.GarantirDisponivel(
            _espacoArmazenamentoService,
            destinationPath,
            estimatedBytes,
            "Não há espaço disponível suficiente para gerar a capa.");
    }
}
