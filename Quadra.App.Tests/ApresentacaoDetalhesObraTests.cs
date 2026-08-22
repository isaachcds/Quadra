using Quadra.App.Presentation;

namespace Quadra.App.Tests;

public sealed class ApresentacaoDetalhesObraTests
{
    [Theory]
    [InlineData("CBR", 12, "12 páginas")]
    [InlineData("CBZ", 12, "12 páginas")]
    [InlineData("PDF", 12, "12 páginas")]
    [InlineData("EPUB", 12, "12 capítulos")]
    public void FormatTotal_UsesCorrectUnit(string format, int total, string expected)
    {
        Assert.Equal(expected, ApresentacaoDetalhesObra.FormatarTotal(format, total));
    }

    [Fact]
    public void CalculateProgress_NotStarted_ReturnsZeroAndStartAction()
    {
        var result = ApresentacaoDetalhesObra.CalcularProgresso("PDF", 0, 10, false);

        Assert.Equal(EstadoProgressoLeitura.NaoIniciada, result.Estado);
        Assert.Equal(0, result.Percentual);
        Assert.Equal("Não iniciado", result.TextoStatus);
        Assert.Equal("Começar leitura", result.TextoBotao);
    }

    [Fact]
    public void CalculateProgress_InProgress_ReturnsPositionAndContinueAction()
    {
        var result = ApresentacaoDetalhesObra.CalcularProgresso("EPUB", 4, 10, true);

        Assert.Equal(EstadoProgressoLeitura.EmAndamento, result.Estado);
        Assert.Equal(0.5, result.Percentual);
        Assert.Equal("Capítulo 5 de 10", result.TextoPosicao);
        Assert.Equal("Continuar leitura", result.TextoBotao);
    }

    [Fact]
    public void CalculateProgress_Completed_ReturnsFullAndReadAgainAction()
    {
        var result = ApresentacaoDetalhesObra.CalcularProgresso("CBZ", 9, 10, true);

        Assert.Equal(EstadoProgressoLeitura.Concluida, result.Estado);
        Assert.Equal(1, result.Percentual);
        Assert.Equal("Concluído", result.TextoStatus);
        Assert.Equal("Ler novamente", result.TextoBotao);
    }

    [Theory]
    [InlineData(-8, 10, 0.1)]
    [InlineData(99, 10, 1)]
    public void CalculateProgress_ClampsPercentage(int current, int total, double expected)
    {
        var result = ApresentacaoDetalhesObra.CalcularProgresso("PDF", current, total, true);

        Assert.InRange(result.Percentual, 0, 1);
        Assert.Equal(expected, result.Percentual);
    }

    [Theory]
    [InlineData(1024L, "1 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    public void FormatFileSize_UsesReadableUnits(long bytes, string expected)
    {
        Assert.Equal(expected, ApresentacaoDetalhesObra.FormatarTamanhoArquivo(bytes));
    }

    [Fact]
    public void FormatFileSize_MissingFileIsUnavailable()
    {
        Assert.Equal("Indisponível", ApresentacaoDetalhesObra.FormatarTamanhoArquivo(null));
    }

    [Fact]
    public void IsFileAvailable_MissingFileReturnsFalse()
    {
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"quadra-missing-{Guid.NewGuid():N}.pdf");

        Assert.False(ApresentacaoDetalhesObra.ArquivoDisponivel(missingPath));
    }
}
