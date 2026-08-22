using Quadra.App.Presentation;

namespace Quadra.App.Tests;

public class CalculadoraProgressoLeituraTests
{
    [Fact]
    public void Calculate_WithNoTotal_IsNotStarted()
    {
        var result = CalculadoraProgressoLeitura.Calcular(0, 0, UnidadeProgressoLeitura.Pagina);

        Assert.Equal(EstadoProgressoLeitura.NaoIniciada, result.Estado);
        Assert.Equal(0, result.Percentual);
    }

    [Fact]
    public void Calculate_FirstPage_PreservesStartStateText()
    {
        var result = CalculadoraProgressoLeitura.Calcular(0, 10, UnidadeProgressoLeitura.Pagina);

        Assert.Equal(EstadoProgressoLeitura.EmAndamento, result.Estado);
        Assert.Equal("Página 1 de 10", result.Texto);
        Assert.Equal("Começar leitura", result.TextoBotao);
    }

    [Fact]
    public void Calculate_InProgress_UsesContinue()
    {
        var result = CalculadoraProgressoLeitura.Calcular(4, 10, UnidadeProgressoLeitura.Pagina);

        Assert.Equal("Página 5 de 10", result.Texto);
        Assert.Equal("Continuar leitura", result.TextoBotao);
    }

    [Fact]
    public void Calculate_LastPage_IsCompleted()
    {
        var result = CalculadoraProgressoLeitura.Calcular(9, 10, UnidadeProgressoLeitura.Pagina);

        Assert.Equal(EstadoProgressoLeitura.Concluida, result.Estado);
        Assert.Equal(1, result.Percentual);
        Assert.Equal("Ler novamente", result.TextoBotao);
    }

    [Fact]
    public void Calculate_PositionBeyondTotal_IsClampedAndCompleted()
    {
        var result = CalculadoraProgressoLeitura.Calcular(99, 10, UnidadeProgressoLeitura.Pagina);

        Assert.Equal(10, result.PosicaoExibida);
        Assert.Equal(1, result.Percentual);
        Assert.Equal(EstadoProgressoLeitura.Concluida, result.Estado);
    }

    [Fact]
    public void Calculate_Epub_UsesChapterLabel()
    {
        var result = CalculadoraProgressoLeitura.Calcular(1, 5, UnidadeProgressoLeitura.Capitulo);

        Assert.Equal("Capítulo 2 de 5", result.Texto);
    }
}
