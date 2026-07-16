namespace Quadra.App.Services;

public enum LibraryFormatFilter
{
    All,
    Epub,
    Pdf,
    Comics
}

public sealed record LibraryProgressInfo(
    ReadingProgressState State,
    double Percentage,
    string Text,
    bool ShowsProgress);

public static class LibraryPresentationLogic
{
    public static IReadOnlyList<T> Filter<T>(
        IEnumerable<T> items,
        LibraryFormatFilter filter,
        Func<T, string> formatSelector)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(formatSelector);

        return items.Where(item => Matches(
                formatSelector(item),
                filter))
            .ToList();
    }

    public static T? SelectContinueReading<T>(
        IEnumerable<T> items,
        Func<T, int> currentSelector,
        Func<T, int> totalSelector,
        Func<T, DateTime?> lastReadSelector)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items
            .Where(item =>
            {
                var current = currentSelector(item);
                var total = totalSelector(item);
                var lastRead = lastReadSelector(item);

                return total > 0 &&
                       current > 0 &&
                       current < total - 1 &&
                       lastRead.HasValue;
            })
            .OrderByDescending(lastReadSelector)
            .FirstOrDefault();
    }

    public static LibraryProgressInfo CalculateProgress(
        int currentPosition,
        int totalPositions)
    {
        if (currentPosition <= 0 || totalPositions <= 0)
        {
            return new LibraryProgressInfo(
                ReadingProgressState.NotStarted,
                0,
                "Não iniciado",
                false);
        }

        if (currentPosition >= totalPositions - 1)
        {
            return new LibraryProgressInfo(
                ReadingProgressState.Completed,
                1,
                "Concluído",
                true);
        }

        var percentage = Math.Clamp(
            (double)(currentPosition + 1) / totalPositions,
            0,
            1);

        return new LibraryProgressInfo(
            ReadingProgressState.InProgress,
            percentage,
            $"{percentage * 100:0}% lido",
            true);
    }

    private static bool Matches(
        string format,
        LibraryFormatFilter filter)
    {
        return filter switch
        {
            LibraryFormatFilter.All => true,
            LibraryFormatFilter.Epub => format.Equals(
                "EPUB",
                StringComparison.OrdinalIgnoreCase),
            LibraryFormatFilter.Pdf => format.Equals(
                "PDF",
                StringComparison.OrdinalIgnoreCase),
            LibraryFormatFilter.Comics =>
                format.Equals("CBR", StringComparison.OrdinalIgnoreCase) ||
                format.Equals("CBZ", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
