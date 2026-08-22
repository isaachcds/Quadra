using Quadra.App.Models;
using Quadra.App.Infrastructure;
using Quadra.App.Policies;
using Quadra.App.Services.Storage;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using VersOne.Epub;

namespace Quadra.App.Services.Readers;

public class LeitorEpubService : ILeitorEpubService
{
    private readonly string _diretorioCacheEpub;
    private readonly IEspacoArmazenamentoService _espacoArmazenamentoService;

    public LeitorEpubService(IEspacoArmazenamentoService espacoArmazenamentoService)
    {
        _espacoArmazenamentoService = espacoArmazenamentoService;
        _diretorioCacheEpub = Path.Combine(
            FileSystem.Current.CacheDirectory,
            "EpubBooks");

        Directory.CreateDirectory(
            _diretorioCacheEpub);
    }

    public async Task<List<EpubChapter>> CarregarCapitulosAsync(
        ObraBiblioteca item,
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

        await ExtrairObraAsync(
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

            var localFilePath = EpubPathResolver.ResolveInsideRoot(
                itemCacheDirectory,
                contentFile.FilePath);

            if (!File.Exists(localFilePath))
                continue;

            var chapterNumber = chapters.Count + 1;

            var chapterTitle = await ExtractChapterTitleAsync(
                localFilePath,
                chapterNumber,
                cancellationToken);

            chapters.Add(new EpubChapter
            {
                Index = chapters.Count,
                Title = chapterTitle,
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

    public void LimparCache(ObraBiblioteca item)
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

    public string ObterRaizConteudo(ObraBiblioteca item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetItemCacheDirectory(item);
    }

    private async Task ExtrairObraAsync(
        string epubFilePath,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        var completedMarker = Path.Combine(
            destinationDirectory,
            ".extraction-complete");

        if (File.Exists(completedMarker) &&
            new FileInfo(completedMarker).Length > 0)
            return;

        if (Directory.Exists(destinationDirectory))
        {
            Directory.Delete(
                destinationDirectory,
                recursive: true);
        }

        Directory.CreateDirectory(
            destinationDirectory);

        try
        {
            using var archive = ZipFile.OpenRead(epubFilePath);

            if (archive.Entries.Count > FileProcessingLimits.MaximumArchiveEntries)
                throw new InvalidDataException("O EPUB possui entradas demais.");

            long expandedBytes = 0;

            foreach (var entry in archive.Entries)
            {
                expandedBytes = checked(expandedBytes + entry.Length);

                if (expandedBytes > FileProcessingLimits.MaximumExpandedBytes)
                    throw new InvalidDataException("O EPUB expandido é muito grande.");
            }

            EnsureCacheSpace(destinationDirectory, expandedBytes);

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var destinationPath = EpubPathResolver.ResolveInsideRoot(
                    destinationDirectory,
                    entry.FullName);

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                EnsureCacheSpace(destinationPath, entry.Length);

                await using var inputStream = entry.Open();

                await AtomicFile.WriteAsync(
                    destinationPath,
                    outputStream => inputStream.CopyToAsync(
                        outputStream,
                        cancellationToken),
                    cancellationToken: cancellationToken);

                if (Path.GetExtension(destinationPath).Equals(
                        ".css",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var css = await File.ReadAllTextAsync(
                        destinationPath,
                        cancellationToken);
                    var sanitizedCss = EpubContentSanitizer.SanitizeCssReferences(
                        css,
                        destinationDirectory,
                        Path.GetDirectoryName(destinationPath) ?? destinationDirectory);

                    if (!string.Equals(css, sanitizedCss, StringComparison.Ordinal))
                    {
                        await AtomicFile.WriteAsync(
                            destinationPath,
                            async outputStream =>
                            {
                                await using var writer = new StreamWriter(
                                    outputStream,
                                    leaveOpen: true);
                                await writer.WriteAsync(sanitizedCss);
                                await writer.FlushAsync(cancellationToken);
                            },
                            cancellationToken: cancellationToken);
                    }
                }
            }

            await AtomicFile.WriteAsync(
                completedMarker,
                async outputStream =>
                {
                    await using var writer = new StreamWriter(
                        outputStream,
                        leaveOpen: true);
                    await writer.WriteAsync(DateTime.UtcNow.ToString("O"));
                    await writer.FlushAsync(cancellationToken);
                },
                cancellationToken: cancellationToken);
        }
        catch
        {
            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, recursive: true);

            throw;
        }
    }

    private string GetItemCacheDirectory(
        ObraBiblioteca item)
    {
        var identifier = item.Id > 0
            ? item.Id.ToString()
            : Path.GetFileNameWithoutExtension(
                item.StoredFileName);

        return Path.Combine(
            _diretorioCacheEpub,
            identifier);
    }

    private void EnsureCacheSpace(string destinationPath, long estimatedBytes)
    {
        PoliticaEspacoArmazenamento.GarantirDisponivel(
            _espacoArmazenamentoService,
            destinationPath,
            estimatedBytes,
            "Não há espaço disponível suficiente para preparar este EPUB.");
    }

    private static async Task<string> ExtractChapterTitleAsync(
    string localFilePath,
    int chapterNumber,
    CancellationToken cancellationToken)
    {
        try
        {
            var html = await File.ReadAllTextAsync(
                localFilePath,
                cancellationToken);

            var headingTitle =
                ExtractFirstHtmlElementContent(
                    html,
                    "h1",
                    "h2",
                    "h3");

            if (!string.IsNullOrWhiteSpace(headingTitle))
                return headingTitle;

            var documentTitle =
                ExtractFirstHtmlElementContent(
                    html,
                    "title");

            if (!string.IsNullOrWhiteSpace(documentTitle) &&
                !IsGenericChapterTitle(documentTitle))
            {
                return documentTitle;
            }
        }
        catch
        {
            // Um título inválido não deve impedir a leitura.
        }

        return $"Capítulo {chapterNumber}";
    }

    private static string? ExtractFirstHtmlElementContent(
    string html,
    params string[] elementNames)
    {
        foreach (var elementName in elementNames)
        {
            var pattern =
                $@"<{elementName}\b[^>]*>(.*?)</{elementName}>";

            var match = Regex.Match(
                html,
                pattern,
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline);

            if (!match.Success)
                continue;

            var content = match.Groups[1].Value;

            content = Regex.Replace(
                content,
                "<[^>]+>",
                " ");

            content = WebUtility.HtmlDecode(content);

            content = Regex.Replace(
                content,
                @"\s+",
                " ");

            content = content.Trim();

            if (!string.IsNullOrWhiteSpace(content))
                return content;
        }

        return null;
    }

    private static bool IsGenericChapterTitle(
    string title)
    {
        var normalizedTitle = title
            .Trim()
            .ToLowerInvariant();

        string[] genericTitles =
        [
            "untitled",
        "sem título",
        "chapter",
        "capítulo",
        "content",
        "conteúdo",
        "document"
        ];

        return genericTitles.Any(generic =>
            normalizedTitle.Equals(
                generic,
                StringComparison.OrdinalIgnoreCase));
    }

}
