using Quadra.App.Services;

namespace Quadra.App.Tests;

public sealed class LibraryPresentationLogicTests
{
    private static readonly TestItem[] Items =
    [
        new("Primeiro", "EPUB", 0, 20, null),
        new("Segundo", "PDF", 4, 10, new DateTime(2026, 1, 1)),
        new("Terceiro", "CBR", 3, 12, new DateTime(2026, 2, 1)),
        new("Quarto", "CBZ", 9, 10, new DateTime(2026, 3, 1))
    ];

    [Theory]
    [InlineData(LibraryFormatFilter.All, 4)]
    [InlineData(LibraryFormatFilter.Epub, 1)]
    [InlineData(LibraryFormatFilter.Pdf, 1)]
    [InlineData(LibraryFormatFilter.Comics, 2)]
    public void Filter_ReturnsExpectedItems(
        LibraryFormatFilter filter,
        int expectedCount)
    {
        var result = LibraryPresentationLogic.Filter(
            Items,
            filter,
            item => item.Format);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Filter_PreservesOriginalOrder()
    {
        var result = LibraryPresentationLogic.Filter(
            Items,
            LibraryFormatFilter.Comics,
            item => item.Format);

        Assert.Equal(["Terceiro", "Quarto"], result.Select(item => item.Title));
    }

    [Fact]
    public void Filter_ReturnsEmptyCollectionWhenFormatIsAbsent()
    {
        var result = LibraryPresentationLogic.Filter(
            Items.Where(item => item.Format == "PDF"),
            LibraryFormatFilter.Epub,
            item => item.Format);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectContinueReading_ReturnsMostRecentEligibleItem()
    {
        var result = SelectContinue(Items);

        Assert.Equal("Terceiro", result?.Title);
    }

    [Fact]
    public void SelectContinueReading_ExcludesCompletedItem()
    {
        var result = SelectContinue([Items[3]]);

        Assert.Null(result);
    }

    [Fact]
    public void SelectContinueReading_ExcludesNotStartedItem()
    {
        var result = SelectContinue([Items[0]]);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0, 0, ReadingProgressState.NotStarted, 0, "Não iniciado")]
    [InlineData(0, 10, ReadingProgressState.NotStarted, 0, "Não iniciado")]
    [InlineData(4, 10, ReadingProgressState.InProgress, 0.5, "50% lido")]
    [InlineData(9, 10, ReadingProgressState.Completed, 1, "Concluído")]
    [InlineData(15, 10, ReadingProgressState.Completed, 1, "Concluído")]
    public void CalculateProgress_ReturnsVisualState(
        int current,
        int total,
        ReadingProgressState state,
        double percentage,
        string text)
    {
        var lastRead = state == ReadingProgressState.NotStarted
            ? (DateTime?)null
            : new DateTime(2026, 1, 1);
        var result = LibraryPresentationLogic.CalculateProgress(current, total, lastRead);

        Assert.Equal(state, result.State);
        Assert.Equal(percentage, result.Percentage);
        Assert.Equal(text, result.Text);
    }

    private static TestItem? SelectContinue(IEnumerable<TestItem> items)
    {
        return LibraryPresentationLogic.SelectContinueReading(
            items,
            item => item.Current,
            item => item.Total,
            item => item.LastReadAt);
    }

    private sealed record TestItem(
        string Title,
        string Format,
        int Current,
        int Total,
        DateTime? LastReadAt);
}
