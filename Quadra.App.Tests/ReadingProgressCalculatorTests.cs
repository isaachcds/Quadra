using Quadra.App.Services;

namespace Quadra.App.Tests;

public class ReadingProgressCalculatorTests
{
    [Fact]
    public void Calculate_WithNoTotal_IsNotStarted()
    {
        var result = ReadingProgressCalculator.Calculate(0, 0, ReadingProgressUnit.Page);

        Assert.Equal(ReadingProgressState.NotStarted, result.State);
        Assert.Equal(0, result.Percentage);
    }

    [Fact]
    public void Calculate_FirstPage_PreservesStartStateText()
    {
        var result = ReadingProgressCalculator.Calculate(0, 10, ReadingProgressUnit.Page);

        Assert.Equal(ReadingProgressState.InProgress, result.State);
        Assert.Equal("Página 1 de 10", result.Text);
        Assert.Equal("Começar leitura", result.ButtonText);
    }

    [Fact]
    public void Calculate_InProgress_UsesContinue()
    {
        var result = ReadingProgressCalculator.Calculate(4, 10, ReadingProgressUnit.Page);

        Assert.Equal("Página 5 de 10", result.Text);
        Assert.Equal("Continuar leitura", result.ButtonText);
    }

    [Fact]
    public void Calculate_LastPage_IsCompleted()
    {
        var result = ReadingProgressCalculator.Calculate(9, 10, ReadingProgressUnit.Page);

        Assert.Equal(ReadingProgressState.Completed, result.State);
        Assert.Equal(1, result.Percentage);
        Assert.Equal("Ler novamente", result.ButtonText);
    }

    [Fact]
    public void Calculate_PositionBeyondTotal_IsClampedAndCompleted()
    {
        var result = ReadingProgressCalculator.Calculate(99, 10, ReadingProgressUnit.Page);

        Assert.Equal(10, result.DisplayedPosition);
        Assert.Equal(1, result.Percentage);
        Assert.Equal(ReadingProgressState.Completed, result.State);
    }

    [Fact]
    public void Calculate_Epub_UsesChapterLabel()
    {
        var result = ReadingProgressCalculator.Calculate(1, 5, ReadingProgressUnit.Chapter);

        Assert.Equal("Capítulo 2 de 5", result.Text);
    }
}
