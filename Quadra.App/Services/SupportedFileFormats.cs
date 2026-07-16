namespace Quadra.App.Services;

public static class SupportedFileFormats
{
    public static readonly string[] Extensions =
    [
        ".cbr",
        ".cbz",
        ".pdf",
        ".epub"
    ];

    public static bool IsSupported(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        var normalized = extension.StartsWith('.')
            ? extension
            : $".{extension}";

        return Extensions.Contains(
            normalized,
            StringComparer.OrdinalIgnoreCase);
    }

    public static string NormalizeFormat(string extension)
    {
        if (!IsSupported(extension))
            throw new InvalidOperationException("O formato do arquivo não é suportado.");

        return extension.TrimStart('.').ToUpperInvariant();
    }
}
