using System.Globalization;
using System.Text;

namespace Quadra.App.Services;

public enum LibraryFormatFilter
{
    All,
    Epub,
    Pdf,
    Comics
}

public enum LibraryReadingStatusFilter
{
    All,
    NotStarted,
    InProgress,
    Completed
}

public enum LibrarySortOption
{
    RecentlyImported,
    LastRead,
    TitleAscending,
    TitleDescending,
    ProgressAscending,
    ProgressDescending
}

public sealed record LibraryFilterCriteria(
    string SearchText,
    LibraryFormatFilter Format,
    LibraryReadingStatusFilter Status,
    LibrarySortOption Sort);

public sealed record LibraryProgressInfo(
    ReadingProgressState State,
    double Percentage,
    string Text,
    bool ShowsProgress);

public static class LibraryPresentationLogic
{
    public static IReadOnlyList<T> ApplyPipeline<T>(
        IEnumerable<T> source,
        LibraryFilterCriteria criteria,
        Func<T, string> titleSelector,
        Func<T, string> originalFileNameSelector,
        Func<T, string> formatSelector,
        Func<T, int> currentSelector,
        Func<T, int> totalSelector,
        Func<T, DateTime> importedAtSelector,
        Func<T, DateTime?> lastReadSelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(criteria);

        var normalizedSearch = NormalizeText(criteria.SearchText.Trim());
        IEnumerable<T> result = source;

        if (normalizedSearch.Length > 0)
        {
            result = result.Where(item =>
                ContainsNormalized(titleSelector(item), normalizedSearch) ||
                ContainsNormalized(originalFileNameSelector(item), normalizedSearch) ||
                ContainsNormalized(formatSelector(item), normalizedSearch));
        }

        result = result.Where(item => MatchesFormat(
            formatSelector(item),
            criteria.Format));

        result = result.Where(item => MatchesStatus(
            GetReadingStatus(
                currentSelector(item),
                totalSelector(item),
                lastReadSelector(item)),
            criteria.Status));

        return Sort(
                result,
                criteria.Sort,
                titleSelector,
                currentSelector,
                totalSelector,
                importedAtSelector,
                lastReadSelector)
            .ToList();
    }

    public static IReadOnlyList<T> Filter<T>(
        IEnumerable<T> items,
        LibraryFormatFilter filter,
        Func<T, string> formatSelector)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(formatSelector);

        return items.Where(item => MatchesFormat(formatSelector(item), filter)).ToList();
    }

    public static T? SelectContinueReading<T>(
        IEnumerable<T> items,
        Func<T, int> currentSelector,
        Func<T, int> totalSelector,
        Func<T, DateTime?> lastReadSelector)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items
            .Where(item => GetReadingStatus(
                    currentSelector(item),
                    totalSelector(item),
                    lastReadSelector(item)) == ReadingProgressState.InProgress &&
                currentSelector(item) > 0)
            .OrderByDescending(lastReadSelector)
            .FirstOrDefault();
    }

    public static LibraryProgressInfo CalculateProgress(
        int currentPosition,
        int totalPositions,
        DateTime? lastReadAt = null)
    {
        var state = GetReadingStatus(currentPosition, totalPositions, lastReadAt);

        if (state == ReadingProgressState.NotStarted)
        {
            return new LibraryProgressInfo(
                state,
                0,
                "Não iniciado",
                false);
        }

        if (state == ReadingProgressState.Completed)
        {
            return new LibraryProgressInfo(
                state,
                1,
                "Concluído",
                true);
        }

        var percentage = CalculatePercentage(currentPosition, totalPositions);
        return new LibraryProgressInfo(
            state,
            percentage,
            $"{percentage * 100:0}% lido",
            true);
    }

    public static ReadingProgressState GetReadingStatus(
        int currentPosition,
        int totalPositions,
        DateTime? lastReadAt)
    {
        if (!lastReadAt.HasValue || totalPositions <= 0)
            return ReadingProgressState.NotStarted;

        return currentPosition >= totalPositions - 1
            ? ReadingProgressState.Completed
            : ReadingProgressState.InProgress;
    }

    public static double CalculatePercentage(int currentPosition, int totalPositions)
    {
        if (totalPositions <= 0)
            return 0;

        return Math.Clamp(
            (double)(currentPosition + 1) / totalPositions,
            0,
            1);
    }

    public static double CalculateEffectivePercentage(
        int currentPosition,
        int totalPositions,
        DateTime? lastReadAt)
    {
        return GetReadingStatus(currentPosition, totalPositions, lastReadAt) switch
        {
            ReadingProgressState.NotStarted => 0,
            ReadingProgressState.Completed => 1,
            _ => CalculatePercentage(currentPosition, totalPositions)
        };
    }

    public static int CountActiveFilters(
        LibraryFormatFilter format,
        LibraryReadingStatusFilter status,
        LibrarySortOption sort)
    {
        var count = 0;
        if (format != LibraryFormatFilter.All)
            count++;
        if (status != LibraryReadingStatusFilter.All)
            count++;
        if (sort != LibrarySortOption.RecentlyImported)
            count++;
        return count;
    }

    public static bool IsLibraryEmpty(int sourceCount) => sourceCount == 0;

    public static bool IsFilteredEmpty(int sourceCount, int visibleCount)
    {
        return sourceCount > 0 && visibleCount == 0;
    }

    public static LibrarySortOption ParseSortOption(int value)
    {
        return Enum.IsDefined(typeof(LibrarySortOption), value)
            ? (LibrarySortOption)value
            : LibrarySortOption.RecentlyImported;
    }

    public static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static IEnumerable<T> Sort<T>(
        IEnumerable<T> items,
        LibrarySortOption sort,
        Func<T, string> titleSelector,
        Func<T, int> currentSelector,
        Func<T, int> totalSelector,
        Func<T, DateTime> importedAtSelector,
        Func<T, DateTime?> lastReadSelector)
    {
        var titleComparer = NormalizedNaturalComparer.Instance;

        return sort switch
        {
            LibrarySortOption.LastRead => items
                .OrderBy(item => lastReadSelector(item).HasValue ? 0 : 1)
                .ThenByDescending(lastReadSelector)
                .ThenByDescending(importedAtSelector)
                .ThenBy(titleSelector, titleComparer),
            LibrarySortOption.TitleAscending => items
                .OrderBy(titleSelector, titleComparer)
                .ThenByDescending(importedAtSelector),
            LibrarySortOption.TitleDescending => items
                .OrderByDescending(titleSelector, titleComparer)
                .ThenByDescending(importedAtSelector),
            LibrarySortOption.ProgressAscending => items
                .OrderBy(item => CalculateEffectivePercentage(
                    currentSelector(item),
                    totalSelector(item),
                    lastReadSelector(item)))
                .ThenBy(titleSelector, titleComparer),
            LibrarySortOption.ProgressDescending => items
                .OrderByDescending(item => CalculateEffectivePercentage(
                    currentSelector(item),
                    totalSelector(item),
                    lastReadSelector(item)))
                .ThenBy(titleSelector, titleComparer),
            _ => items
                .OrderByDescending(importedAtSelector)
                .ThenBy(titleSelector, titleComparer)
        };
    }

    private static bool ContainsNormalized(string? value, string normalizedSearch)
    {
        return NormalizeText(value).Contains(normalizedSearch, StringComparison.Ordinal);
    }

    private static bool MatchesStatus(
        ReadingProgressState state,
        LibraryReadingStatusFilter filter)
    {
        return filter switch
        {
            LibraryReadingStatusFilter.All => true,
            LibraryReadingStatusFilter.NotStarted => state == ReadingProgressState.NotStarted,
            LibraryReadingStatusFilter.InProgress => state == ReadingProgressState.InProgress,
            LibraryReadingStatusFilter.Completed => state == ReadingProgressState.Completed,
            _ => false
        };
    }

    private static bool MatchesFormat(string format, LibraryFormatFilter filter)
    {
        return filter switch
        {
            LibraryFormatFilter.All => true,
            LibraryFormatFilter.Epub => format.Equals("EPUB", StringComparison.OrdinalIgnoreCase),
            LibraryFormatFilter.Pdf => format.Equals("PDF", StringComparison.OrdinalIgnoreCase),
            LibraryFormatFilter.Comics =>
                format.Equals("CBR", StringComparison.OrdinalIgnoreCase) ||
                format.Equals("CBZ", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private sealed class NormalizedNaturalComparer : IComparer<string>
    {
        public static NormalizedNaturalComparer Instance { get; } = new();

        public int Compare(string? x, string? y)
        {
            return NaturalStringComparer.Instance.Compare(
                NormalizeText(x),
                NormalizeText(y));
        }
    }
}
