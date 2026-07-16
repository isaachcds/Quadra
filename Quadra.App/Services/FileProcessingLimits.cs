namespace Quadra.App.Services;

public static class FileProcessingLimits
{
    // Valores conservadores para bibliotecas pessoais sem bloquear livros comuns.
    public const long MaximumImportBytes = 4L * 1024 * 1024 * 1024;
    public const int MaximumArchiveEntries = 20_000;
    public const long MaximumExpandedBytes = 16L * 1024 * 1024 * 1024;
    public const int MaximumPages = 10_000;
    public const int MaximumPathLength = 512;
    public const long MaximumPdfBitmapPixels = 40_000_000;
    public const int MaximumPdfBitmapHeight = 16_000;
    public const int PdfTargetWidth = 1_400;
    public const long MinimumFreeSpaceBytes = 512L * 1024 * 1024;

    public static void EnsureFreeSpace(string destinationPath)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(destinationPath));

        if (string.IsNullOrWhiteSpace(root))
            return;

        var drive = new DriveInfo(root);

        if (drive.AvailableFreeSpace < MinimumFreeSpaceBytes)
        {
            throw new IOException(
                "Não há espaço livre suficiente para processar este arquivo.");
        }
    }
}
