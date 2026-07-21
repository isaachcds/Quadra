using System.Globalization;

namespace Quadra.App.Services;

public sealed record BookDetailsProgress(
    ReadingProgressState State,
    double Percentage,
    string StatusText,
    string PositionText,
    string ButtonText);

public static class BookDetailsPresentation
{
    public static bool IsFileAvailable(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    public static ReadingProgressUnit GetProgressUnit(string? format)
    {
        return string.Equals(format, "EPUB", StringComparison.OrdinalIgnoreCase)
            ? ReadingProgressUnit.Chapter
            : ReadingProgressUnit.Page;
    }

    public static string FormatTotal(string? format, int total)
    {
        var safeTotal = Math.Max(0, total);
        var unit = GetProgressUnit(format);
        var label = unit == ReadingProgressUnit.Chapter
            ? safeTotal == 1 ? "capítulo" : "capítulos"
            : safeTotal == 1 ? "página" : "páginas";

        return $"{safeTotal} {label}";
    }

    public static BookDetailsProgress CalculateProgress(
        string? format,
        int currentPosition,
        int totalPositions,
        bool hasBeenRead)
    {
        if (!hasBeenRead || totalPositions <= 0)
        {
            return new BookDetailsProgress(
                ReadingProgressState.NotStarted,
                0,
                "Não iniciado",
                FormatTotal(format, totalPositions),
                "Começar leitura");
        }

        var progress = ReadingProgressCalculator.Calculate(
            currentPosition,
            totalPositions,
            GetProgressUnit(format));

        var status = progress.State == ReadingProgressState.Completed
            ? "Concluído"
            : "Em andamento";

        return new BookDetailsProgress(
            progress.State,
            Math.Clamp(progress.Percentage, 0, 1),
            status,
            progress.Text,
            progress.ButtonText);
    }

    public static string FormatFileSize(long? bytes)
    {
        if (bytes is null || bytes < 0)
            return "Indisponível";

        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes.Value;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        var format = unitIndex == 0 ? "0" : "0.#";
        return $"{value.ToString(format, CultureInfo.CurrentCulture)} {units[unitIndex]}";
    }
}
