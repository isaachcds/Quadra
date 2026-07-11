using Android.Graphics.Pdf;
using Android.OS;
using Quadra.App.Models;
using Quadra.App.Services;

using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidColor = Android.Graphics.Color;
using SystemFile = System.IO.File;
using SystemPath = System.IO.Path;

namespace Quadra.App.Platforms.Android.Services;

public class PdfReaderService : IPdfReaderService
{
    private const int TargetWidth = 1400;

    private readonly string _pdfCacheDirectory;

    public PdfReaderService()
    {
        _pdfCacheDirectory = SystemPath.Combine(
            FileSystem.Current.CacheDirectory,
            "PdfPages");

        Directory.CreateDirectory(_pdfCacheDirectory);
    }

    public async Task<List<ComicPage>> LoadPagesAsync(
        LibraryItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!SystemFile.Exists(item.FilePath))
        {
            throw new FileNotFoundException(
                "O arquivo PDF não foi encontrado.",
                item.FilePath);
        }

        return await Task.Run(
            () => RenderPages(
                item,
                cancellationToken),
            cancellationToken);
    }

    public void ClearCache(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var directory = GetItemCacheDirectory(item);

        if (Directory.Exists(directory))
        {
            Directory.Delete(
                directory,
                recursive: true);
        }
    }

    private List<ComicPage> RenderPages(
        LibraryItem item,
        CancellationToken cancellationToken)
    {
        var itemCacheDirectory =
            GetItemCacheDirectory(item);

        Directory.CreateDirectory(
            itemCacheDirectory);

        using var javaFile =
            new Java.IO.File(item.FilePath);

        using var descriptor =
            ParcelFileDescriptor.Open(
                javaFile,
                ParcelFileMode.ReadOnly);

        if (descriptor is null)
        {
            throw new InvalidOperationException(
                "Não foi possível abrir o PDF.");
        }

        // PdfRenderer assume o controle do descritor.
        using var renderer =
            new PdfRenderer(descriptor);

        var pages = new List<ComicPage>(
            renderer.PageCount);

        for (var index = 0;
             index < renderer.PageCount;
             index++)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var fileName =
                $"{index:D5}.png";

            var destinationPath =
                SystemPath.Combine(
                    itemCacheDirectory,
                    fileName);

            if (!SystemFile.Exists(destinationPath))
            {
                RenderPage(
                    renderer,
                    index,
                    destinationPath,
                    cancellationToken);
            }

            pages.Add(new ComicPage
            {
                Index = index,
                FileName = fileName,
                FilePath = destinationPath
            });
        }

        return pages;
    }

    private static void RenderPage(
        PdfRenderer renderer,
        int pageIndex,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        using var page =
            renderer.OpenPage(pageIndex);

        if (page is null ||
            page.Width <= 0 ||
            page.Height <= 0)
        {
            throw new InvalidOperationException(
                $"Não foi possível renderizar a página {pageIndex + 1}.");
        }

        var scale =
            (double)TargetWidth / page.Width;

        var targetHeight = Math.Max(
            1,
            (int)Math.Round(
                page.Height * scale));

        using var bitmap =
            AndroidBitmap.CreateBitmap(
                TargetWidth,
                targetHeight,
                AndroidBitmap.Config.Argb8888);

        if (bitmap is null)
        {
            throw new InvalidOperationException(
                $"Não foi possível criar a imagem da página {pageIndex + 1}.");
        }

        bitmap.EraseColor(
            AndroidColor.White);

        page.Render(
            bitmap,
            null,
            null,
            PdfRenderMode.ForDisplay);

        cancellationToken
            .ThrowIfCancellationRequested();

        using var outputStream =
            new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

        var saved = bitmap.Compress(
            AndroidBitmap.CompressFormat.Png,
            100,
            outputStream);

        if (!saved)
        {
            throw new InvalidOperationException(
                $"Não foi possível salvar a página {pageIndex + 1}.");
        }

        outputStream.Flush();
    }

    private string GetItemCacheDirectory(
        LibraryItem item)
    {
        var identifier = item.Id > 0
            ? item.Id.ToString()
            : SystemPath.GetFileNameWithoutExtension(
                item.StoredFileName);

        return SystemPath.Combine(
            _pdfCacheDirectory,
            identifier);
    }
}