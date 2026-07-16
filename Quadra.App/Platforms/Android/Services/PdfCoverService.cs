using Android.Graphics.Pdf;
using Android.OS;
using Quadra.App.Models;
using Quadra.App.Services;

using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidColor = Android.Graphics.Color;
using SystemFile = System.IO.File;
using SystemPath = System.IO.Path;

namespace Quadra.App.Platforms.Android.Services;

public class PdfCoverService : IPdfCoverService
{
    private const int TargetWidth = 600;

    public async Task<string?> GenerateCoverAsync(
        LibraryItem item,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!SystemFile.Exists(item.FilePath))
            return null;

        return await Task.Run(
            () => RenderFirstPage(
                item.FilePath,
                destinationPath,
                cancellationToken),
            cancellationToken);
    }

    private static string? RenderFirstPage(
        string pdfPath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var javaFile =
            new Java.IO.File(pdfPath);

        using var descriptor =
            ParcelFileDescriptor.Open(
                javaFile,
                ParcelFileMode.ReadOnly);

        if (descriptor is null)
            return null;

        using var renderer =
            new PdfRenderer(descriptor);

        if (renderer.PageCount <= 0)
            return null;

        using var page =
            renderer.OpenPage(0);

        if (page is null ||
            page.Width <= 0 ||
            page.Height <= 0)
        {
            return null;
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
                AndroidBitmap.Config.Argb8888!);

        if (bitmap is null)
            return null;

        bitmap.EraseColor(
            AndroidColor.White);

        page.Render(
            bitmap,
            null,
            null,
            PdfRenderMode.ForDisplay);

        cancellationToken.ThrowIfCancellationRequested();

        var directory =
            SystemPath.GetDirectoryName(
                destinationPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        AtomicFile.Write(
            destinationPath,
            outputStream =>
            {
                if (!bitmap.Compress(
                        AndroidBitmap.CompressFormat.Png!,
                        100,
                        outputStream))
                {
                    throw new InvalidOperationException(
                        "Não foi possível salvar a capa do PDF.");
                }
            },
            cancellationToken);

        return destinationPath;
    }
}
