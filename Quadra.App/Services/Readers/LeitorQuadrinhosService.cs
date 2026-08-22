using Quadra.App.Models;
using Quadra.App.Infrastructure;
using Quadra.App.Policies;
using Quadra.App.Services.Storage;
using SharpCompress.Archives;

namespace Quadra.App.Services.Readers;

public class LeitorQuadrinhosService
{
    private static readonly string[] ImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp"
    ];

    private readonly string _diretorioCacheQuadrinhos;
    private readonly ILeitorPdfService _leitorPdfService;
    private readonly IEspacoArmazenamentoService _espacoArmazenamentoService;

    public LeitorQuadrinhosService(
        ILeitorPdfService leitorPdfService,
        IEspacoArmazenamentoService espacoArmazenamentoService)
    {
        _leitorPdfService = leitorPdfService;
        _espacoArmazenamentoService = espacoArmazenamentoService;

        _diretorioCacheQuadrinhos = Path.Combine(
            FileSystem.Current.CacheDirectory,
            "Comics");

        Directory.CreateDirectory(
            _diretorioCacheQuadrinhos);
    }

    public async Task<List<ComicPage>> CarregarPaginasAsync(
        ObraBiblioteca item,
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

            "PDF" => await _leitorPdfService.CarregarPaginasAsync(
                item,
                cancellationToken),

            _ => throw new NotSupportedException(
                $"O formato {item.Format} ainda não possui leitor.")
        };
    }

    private async Task<List<ComicPage>> LoadCbrPagesAsync(
        ObraBiblioteca item,
        CancellationToken cancellationToken)
    {
        var itemCacheDirectory = PrepareItemCache(item);

        using var archive =
            ArchiveFactory.OpenArchive(item.FilePath);

        var expandedBytes = archive.Entries.Sum(entry => Math.Max(0, entry.Size));
        EnsureArchiveLimits(archive.Entries.Count(), expandedBytes);
        EnsureCacheSpace(itemCacheDirectory, expandedBytes);

        var imageEntries = archive.Entries
            .Where(entry =>
                !entry.IsDirectory &&
                !string.IsNullOrWhiteSpace(entry.Key) &&
                IsImage(entry.Key))
            .OrderBy(
                entry => entry.Key!,
                NaturalStringComparer.Instance)
            .ToList();

        EnsurePageCount(imageEntries.Count);

        var pages = new List<ComicPage>();

        for (var index = 0; index < imageEntries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = imageEntries[index];
            var entryKey = entry.Key!;

            var extension = Path
                .GetExtension(entryKey)
                .ToLowerInvariant();

            var destinationFileName =
                $"{index:D5}{extension}";

            var destinationPath = Path.Combine(
                itemCacheDirectory,
                destinationFileName);

            if (!IsValidCachedFile(destinationPath))
            {
                AtomicFile.DeleteIfExists(destinationPath);
                EnsureCacheSpace(destinationPath, Math.Max(0, entry.Size));
                await using var inputStream = entry.OpenEntryStream();

                await AtomicFile.WriteAsync(
                    destinationPath,
                    outputStream => inputStream.CopyToAsync(
                        outputStream,
                        cancellationToken),
                    cancellationToken: cancellationToken);
            }

            pages.Add(new ComicPage
            {
                Index = index,
                FileName = entryKey,
                FilePath = destinationPath
            });
        }

        return pages;
    }

    private async Task<List<ComicPage>> LoadCbzPagesAsync(
     ObraBiblioteca item,
     CancellationToken cancellationToken)
    {
        var itemCacheDirectory = PrepareItemCache(item);

        using var archive =
            System.IO.Compression.ZipFile.OpenRead(item.FilePath);

        var expandedBytes = archive.Entries.Sum(entry => Math.Max(0, entry.Length));
        EnsureArchiveLimits(archive.Entries.Count, expandedBytes);
        EnsureCacheSpace(itemCacheDirectory, expandedBytes);

        var imageEntries = archive.Entries
            .Where(entry =>
                !string.IsNullOrWhiteSpace(entry.Name) &&
                IsImage(entry.FullName))
            .OrderBy(
                entry => entry.FullName,
                NaturalStringComparer.Instance)
            .ToList();

        EnsurePageCount(imageEntries.Count);

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

            if (!IsValidCachedFile(destinationPath))
            {
                AtomicFile.DeleteIfExists(destinationPath);
                EnsureCacheSpace(destinationPath, Math.Max(0, entry.Length));
                await using var inputStream = entry.Open();

                await AtomicFile.WriteAsync(
                    destinationPath,
                    outputStream => inputStream.CopyToAsync(
                        outputStream,
                        cancellationToken),
                    cancellationToken: cancellationToken);
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

    public void LimparCache(ObraBiblioteca item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Format.Equals(
            "PDF",
            StringComparison.OrdinalIgnoreCase))
        {
            _leitorPdfService.LimparCache(item);
            return;
        }

        var directory =
            GetItemCacheDirectory(item);

        if (Directory.Exists(directory))
        {
            Directory.Delete(
                directory,
                recursive: true);
        }
    }

    private string PrepareItemCache(ObraBiblioteca item)
    {
        var directory = GetItemCacheDirectory(item);

        Directory.CreateDirectory(directory);

        return directory;
    }

    private string GetItemCacheDirectory(
        ObraBiblioteca item)
    {
        var identifier = item.Id > 0
            ? item.Id.ToString()
            : Path.GetFileNameWithoutExtension(
                item.StoredFileName);

        return Path.Combine(
            _diretorioCacheQuadrinhos,
            identifier);
    }

    private static bool IsImage(string fileName)
    {
        var extension = Path
            .GetExtension(fileName)
            .ToLowerInvariant();

        return ImageExtensions.Contains(extension);
    }

    private static void EnsurePageCount(int pageCount)
    {
        if (pageCount > FileProcessingLimits.MaximumPages)
            throw new InvalidDataException("A obra possui páginas demais para ser processada.");
    }

    private static void EnsureArchiveLimits(int entryCount, long expandedBytes)
    {
        if (entryCount > FileProcessingLimits.MaximumArchiveEntries)
            throw new InvalidDataException("O arquivo compactado possui entradas demais.");

        if (expandedBytes > FileProcessingLimits.MaximumExpandedBytes)
            throw new InvalidDataException("O conteúdo expandido excede o limite seguro.");
    }

    private static bool IsValidCachedFile(string path)
    {
        return File.Exists(path) && new FileInfo(path).Length > 0;
    }

    private void EnsureCacheSpace(string destinationPath, long estimatedBytes)
    {
        PoliticaEspacoArmazenamento.GarantirDisponivel(
            _espacoArmazenamentoService,
            destinationPath,
            estimatedBytes,
            "Não há espaço disponível suficiente para preparar esta obra.");
    }
}
