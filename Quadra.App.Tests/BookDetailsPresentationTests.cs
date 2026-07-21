using Quadra.App.Services;

namespace Quadra.App.Tests;

public sealed class BookDetailsPresentationTests
{
    [Theory]
    [InlineData("CBR", 12, "12 páginas")]
    [InlineData("CBZ", 12, "12 páginas")]
    [InlineData("PDF", 12, "12 páginas")]
    [InlineData("EPUB", 12, "12 capítulos")]
    public void FormatTotal_UsesCorrectUnit(string format, int total, string expected)
    {
        Assert.Equal(expected, BookDetailsPresentation.FormatTotal(format, total));
    }

    [Fact]
    public void CalculateProgress_NotStarted_ReturnsZeroAndStartAction()
    {
        var result = BookDetailsPresentation.CalculateProgress("PDF", 0, 10, false);

        Assert.Equal(ReadingProgressState.NotStarted, result.State);
        Assert.Equal(0, result.Percentage);
        Assert.Equal("Não iniciado", result.StatusText);
        Assert.Equal("Começar leitura", result.ButtonText);
    }

    [Fact]
    public void CalculateProgress_InProgress_ReturnsPositionAndContinueAction()
    {
        var result = BookDetailsPresentation.CalculateProgress("EPUB", 4, 10, true);

        Assert.Equal(ReadingProgressState.InProgress, result.State);
        Assert.Equal(0.5, result.Percentage);
        Assert.Equal("Capítulo 5 de 10", result.PositionText);
        Assert.Equal("Continuar leitura", result.ButtonText);
    }

    [Fact]
    public void CalculateProgress_Completed_ReturnsFullAndReadAgainAction()
    {
        var result = BookDetailsPresentation.CalculateProgress("CBZ", 9, 10, true);

        Assert.Equal(ReadingProgressState.Completed, result.State);
        Assert.Equal(1, result.Percentage);
        Assert.Equal("Concluído", result.StatusText);
        Assert.Equal("Ler novamente", result.ButtonText);
    }

    [Theory]
    [InlineData(-8, 10, 0.1)]
    [InlineData(99, 10, 1)]
    public void CalculateProgress_ClampsPercentage(int current, int total, double expected)
    {
        var result = BookDetailsPresentation.CalculateProgress("PDF", current, total, true);

        Assert.InRange(result.Percentage, 0, 1);
        Assert.Equal(expected, result.Percentage);
    }

    [Theory]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    public void FormatFileSize_UsesReadableUnits(long bytes, string expected)
    {
        Assert.Equal(expected, BookDetailsPresentation.FormatFileSize(bytes));
    }

    [Fact]
    public void FormatFileSize_MissingFileIsUnavailable()
    {
        Assert.Equal("Indisponível", BookDetailsPresentation.FormatFileSize(null));
    }

    [Fact]
    public void IsFileAvailable_MissingFileReturnsFalse()
    {
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"quadra-missing-{Guid.NewGuid():N}.pdf");

        Assert.False(BookDetailsPresentation.IsFileAvailable(missingPath));
    }
}
