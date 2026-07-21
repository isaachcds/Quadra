using Quadra.App.Services;

namespace Quadra.App.Tests;

public sealed class LibraryFilterPipelineTests
{
    private static readonly TestItem[] Items =
    [
        new("Batman - Ano Um", "batman_ano_1.cbz", "CBZ", 0, 100, new(2026, 1, 4), null),
        new("O Hóbbit", "o_hobbit.epub", "EPUB", 4, 10, new(2026, 1, 3), new(2026, 3, 1)),
        new("Manual 10", "manual-final.pdf", "PDF", 9, 10, new(2026, 1, 2), new(2026, 2, 1)),
        new("Manual 2", "arquivo-antigo.cbr", "CBR", 1, 10, new(2026, 1, 1), new(2026, 1, 1))
    ];

    [Theory]
    [InlineData("Batman - Ano Um", "Batman - Ano Um")]
    [InlineData("ano um", "Batman - Ano Um")]
    [InlineData("manual-final", "Manual 10")]
    [InlineData("HOBBIT", "O Hóbbit")]
    [InlineData("  batman  ", "Batman - Ano Um")]
    [InlineData("hobbit", "O Hóbbit")]
    public void Search_FindsExpectedItem(string query, string expectedTitle)
    {
        var result = Apply(query: query);

        Assert.Contains(result, item => item.Title == expectedTitle);
    }

    [Fact]
    public void Search_EmptyReturnsAllItems()
    {
        Assert.Equal(Items.Length, Apply(query: "  ").Count);
    }

    [Fact]
    public void Search_WithoutMatchReturnsEmpty()
    {
        Assert.Empty(Apply(query: "inexistente"));
    }

    [Fact]
    public void Pipeline_CombinesSearchAndFormat()
    {
        var result = Apply("manual", LibraryFormatFilter.Pdf);

        Assert.Single(result);
        Assert.Equal("Manual 10", result[0].Title);
    }

    [Fact]
    public void Pipeline_CombinesFormatAndStatus()
    {
        var result = Apply(
            format: LibraryFormatFilter.Comics,
            status: LibraryReadingStatusFilter.InProgress);

        Assert.Single(result);
        Assert.Equal("Manual 2", result[0].Title);
    }

    [Fact]
    public void Pipeline_CombinesSearchAndStatus()
    {
        var result = Apply(
            query: "manual",
            status: LibraryReadingStatusFilter.Completed);

        Assert.Single(result);
        Assert.Equal("Manual 10", result[0].Title);
    }

    [Fact]
    public void Pipeline_CombinesSearchFormatAndStatus()
    {
        var result = Apply(
            "manual",
            LibraryFormatFilter.Comics,
            LibraryReadingStatusFilter.InProgress);

        Assert.Single(result);
        Assert.Equal("Manual 2", result[0].Title);
    }

    [Fact]
    public void DefaultCriteria_RestoresAllItems()
    {
        var result = Apply();

        Assert.Equal(Items.Length, result.Count);
    }

    [Theory]
    [InlineData(0, 10, null, ReadingProgressState.NotStarted)]
    [InlineData(4, 10, "2026-01-01", ReadingProgressState.InProgress)]
    [InlineData(9, 10, "2026-01-01", ReadingProgressState.Completed)]
    [InlineData(2, 0, "2026-01-01", ReadingProgressState.NotStarted)]
    [InlineData(-8, 10, "2026-01-01", ReadingProgressState.InProgress)]
    [InlineData(99, 10, "2026-01-01", ReadingProgressState.Completed)]
    public void Status_UsesCentralProgressRule(
        int current,
        int total,
        string? lastRead,
        ReadingProgressState expected)
    {
        DateTime? date = lastRead is null ? null : DateTime.Parse(lastRead);

        Assert.Equal(
            expected,
            LibraryPresentationLogic.GetReadingStatus(current, total, date));
    }

    [Fact]
    public void Status_EpubUsesSameChapterProgressValues()
    {
        var result = Apply(
            format: LibraryFormatFilter.Epub,
            status: LibraryReadingStatusFilter.InProgress);

        Assert.Single(result);
        Assert.Equal("O Hóbbit", result[0].Title);
    }

    [Theory]
    [InlineData(LibrarySortOption.RecentlyImported, "Batman - Ano Um")]
    [InlineData(LibrarySortOption.LastRead, "O Hóbbit")]
    [InlineData(LibrarySortOption.TitleAscending, "Batman - Ano Um")]
    [InlineData(LibrarySortOption.TitleDescending, "O Hóbbit")]
    [InlineData(LibrarySortOption.ProgressAscending, "Batman - Ano Um")]
    [InlineData(LibrarySortOption.ProgressDescending, "Manual 10")]
    public void Sorting_ReturnsExpectedFirstItem(
        LibrarySortOption sort,
        string expectedFirst)
    {
        Assert.Equal(expectedFirst, Apply(sort: sort)[0].Title);
    }

    [Fact]
    public void Sorting_TitleUsesNaturalNumberOrder()
    {
        var result = Apply(sort: LibrarySortOption.TitleAscending);
        var titles = result.Select(item => item.Title).ToList();

        Assert.True(titles.IndexOf("Manual 2") < titles.IndexOf("Manual 10"));
    }

    [Fact]
    public void Sorting_LastReadPlacesNeverReadLast()
    {
        Assert.Equal("Batman - Ano Um", Apply(sort: LibrarySortOption.LastRead)[^1].Title);
    }

    [Fact]
    public void Sorting_UsesStableTitleTieBreakForProgress()
    {
        var tied =
            new[]
            {
                Items[0] with { Title = "Zulu" },
                Items[0] with { Title = "Alfa" }
            };

        var result = Apply(source: tied, sort: LibrarySortOption.ProgressAscending);

        Assert.Equal(["Alfa", "Zulu"], result.Select(item => item.Title));
    }

    [Theory]
    [InlineData(LibraryFormatFilter.All, LibraryReadingStatusFilter.All, LibrarySortOption.RecentlyImported, 0)]
    [InlineData(LibraryFormatFilter.Pdf, LibraryReadingStatusFilter.All, LibrarySortOption.RecentlyImported, 1)]
    [InlineData(LibraryFormatFilter.Pdf, LibraryReadingStatusFilter.InProgress, LibrarySortOption.RecentlyImported, 2)]
    [InlineData(LibraryFormatFilter.Pdf, LibraryReadingStatusFilter.InProgress, LibrarySortOption.TitleAscending, 3)]
    public void ActiveFilterCount_ExcludesDefaults(
        LibraryFormatFilter format,
        LibraryReadingStatusFilter status,
        LibrarySortOption sort,
        int expected)
    {
        Assert.Equal(expected, LibraryPresentationLogic.CountActiveFilters(format, status, sort));
    }

    [Fact]
    public void EmptyStates_DistinguishLibraryEmptyFromFilteredEmpty()
    {
        Assert.True(LibraryPresentationLogic.IsLibraryEmpty(0));
        Assert.False(LibraryPresentationLogic.IsFilteredEmpty(0, 0));
        Assert.True(LibraryPresentationLogic.IsFilteredEmpty(4, 0));
    }

    [Fact]
    public void InvalidPersistedSort_ReturnsDefault()
    {
        Assert.Equal(
            LibrarySortOption.RecentlyImported,
            LibraryPresentationLogic.ParseSortOption(999));
    }

    [Fact]
    public void ValidPersistedSort_IsRestored()
    {
        Assert.Equal(
            LibrarySortOption.TitleDescending,
            LibraryPresentationLogic.ParseSortOption(
                (int)LibrarySortOption.TitleDescending));
    }

    [Fact]
    public void Pipeline_DoesNotChangeSourceOrder()
    {
        var source = Items.ToList();
        var originalOrder = source.Select(item => item.Title).ToArray();

        _ = Apply(
            query: "manual",
            sort: LibrarySortOption.TitleDescending,
            source: source);

        Assert.Equal(originalOrder, source.Select(item => item.Title));
    }

    private static IReadOnlyList<TestItem> Apply(
        string query = "",
        LibraryFormatFilter format = LibraryFormatFilter.All,
        LibraryReadingStatusFilter status = LibraryReadingStatusFilter.All,
        LibrarySortOption sort = LibrarySortOption.RecentlyImported,
        IEnumerable<TestItem>? source = null)
    {
        return LibraryPresentationLogic.ApplyPipeline(
            source ?? Items,
            new LibraryFilterCriteria(query, format, status, sort),
            item => item.Title,
            item => item.OriginalFileName,
            item => item.Format,
            item => item.Current,
            item => item.Total,
            item => item.ImportedAt,
            item => item.LastReadAt);
    }

    private sealed record TestItem(
        string Title,
        string OriginalFileName,
        string Format,
        int Current,
        int Total,
        DateTime ImportedAt,
        DateTime? LastReadAt);
}
