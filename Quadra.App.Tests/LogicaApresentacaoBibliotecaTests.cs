using Quadra.App.Presentation;

namespace Quadra.App.Tests;

public sealed class LogicaApresentacaoBibliotecaTests
{
    private static readonly TestItem[] Items =
    [
        new("Primeiro", "EPUB", 0, 20, null),
        new("Segundo", "PDF", 4, 10, new DateTime(2026, 1, 1)),
        new("Terceiro", "CBR", 3, 12, new DateTime(2026, 2, 1)),
        new("Quarto", "CBZ", 9, 10, new DateTime(2026, 3, 1))
    ];

    [Theory]
    [InlineData(FiltroFormatoBiblioteca.Todos, 4)]
    [InlineData(FiltroFormatoBiblioteca.Epub, 1)]
    [InlineData(FiltroFormatoBiblioteca.Pdf, 1)]
    [InlineData(FiltroFormatoBiblioteca.Quadrinhos, 2)]
    public void Filter_ReturnsExpectedItems(
        FiltroFormatoBiblioteca filter,
        int expectedCount)
    {
        var result = LogicaApresentacaoBiblioteca.Filtrar(
            Items,
            filter,
            item => item.Formato);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Filter_PreservesOriginalOrder()
    {
        var result = LogicaApresentacaoBiblioteca.Filtrar(
            Items,
            FiltroFormatoBiblioteca.Quadrinhos,
            item => item.Formato);

        Assert.Equal(["Terceiro", "Quarto"], result.Select(item => item.Title));
    }

    [Fact]
    public void Filtrar_RetornaColecaoVaziaQuandoFormatoNaoExiste()
    {
        var result = LogicaApresentacaoBiblioteca.Filtrar(
            Items.Where(item => item.Formato == "PDF"),
            FiltroFormatoBiblioteca.Epub,
            item => item.Formato);

        Assert.Empty(result);
    }

    [Fact]
    public void SelecionarContinuarLeitura_RetornaItemElegivelMaisRecente()
    {
        var result = SelectContinue(Items);

        Assert.Equal("Terceiro", result?.Title);
    }

    [Fact]
    public void SelecionarContinuarLeitura_ExcluiItemConcluido()
    {
        var result = SelectContinue([Items[3]]);

        Assert.Null(result);
    }

    [Fact]
    public void SelecionarContinuarLeitura_ExcluiItemNaoIniciado()
    {
        var result = SelectContinue([Items[0]]);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0, 0, EstadoProgressoLeitura.NaoIniciada, 0, "Não iniciado")]
    [InlineData(0, 10, EstadoProgressoLeitura.NaoIniciada, 0, "Não iniciado")]
    [InlineData(4, 10, EstadoProgressoLeitura.EmAndamento, 0.5, "50% lido")]
    [InlineData(9, 10, EstadoProgressoLeitura.Concluida, 1, "Concluído")]
    [InlineData(15, 10, EstadoProgressoLeitura.Concluida, 1, "Concluído")]
    public void CalculateProgress_ReturnsVisualState(
        int current,
        int total,
        EstadoProgressoLeitura state,
        double percentage,
        string text)
    {
        var lastRead = state == EstadoProgressoLeitura.NaoIniciada
            ? (DateTime?)null
            : new DateTime(2026, 1, 1);
        var result = LogicaApresentacaoBiblioteca.CalcularProgresso(current, total, lastRead);

        Assert.Equal(state, result.Estado);
        Assert.Equal(percentage, result.Percentual);
        Assert.Equal(text, result.Texto);
    }

    private static TestItem? SelectContinue(IEnumerable<TestItem> items)
    {
        return LogicaApresentacaoBiblioteca.SelecionarContinuarLeitura(
            items,
            item => item.Current,
            item => item.Total,
            item => item.LastReadAt);
    }

    private sealed record TestItem(
        string Title,
        string Formato,
        int Current,
        int Total,
        DateTime? LastReadAt);
}
