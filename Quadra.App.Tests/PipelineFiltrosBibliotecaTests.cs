using Quadra.App.Presentation;

namespace Quadra.App.Tests;

public sealed class PipelineFiltrosBibliotecaTests
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
        var result = Apply("manual", FiltroFormatoBiblioteca.Pdf);

        Assert.Single(result);
        Assert.Equal("Manual 10", result[0].Title);
    }

    [Fact]
    public void Pipeline_CombinesFormatAndStatus()
    {
        var result = Apply(
            format: FiltroFormatoBiblioteca.Quadrinhos,
            status: FiltroStatusLeituraBiblioteca.EmAndamento);

        Assert.Single(result);
        Assert.Equal("Manual 2", result[0].Title);
    }

    [Fact]
    public void Pipeline_CombinesSearchAndStatus()
    {
        var result = Apply(
            query: "manual",
            status: FiltroStatusLeituraBiblioteca.Concluida);

        Assert.Single(result);
        Assert.Equal("Manual 10", result[0].Title);
    }

    [Fact]
    public void Pipeline_CombinesSearchFormatAndStatus()
    {
        var result = Apply(
            "manual",
            FiltroFormatoBiblioteca.Quadrinhos,
            FiltroStatusLeituraBiblioteca.EmAndamento);

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
    [InlineData(0, 10, null, EstadoProgressoLeitura.NaoIniciada)]
    [InlineData(4, 10, "2026-01-01", EstadoProgressoLeitura.EmAndamento)]
    [InlineData(9, 10, "2026-01-01", EstadoProgressoLeitura.Concluida)]
    [InlineData(2, 0, "2026-01-01", EstadoProgressoLeitura.NaoIniciada)]
    [InlineData(-8, 10, "2026-01-01", EstadoProgressoLeitura.EmAndamento)]
    [InlineData(99, 10, "2026-01-01", EstadoProgressoLeitura.Concluida)]
    public void Status_UsesCentralProgressRule(
        int current,
        int total,
        string? lastRead,
        EstadoProgressoLeitura expected)
    {
        DateTime? date = lastRead is null ? null : DateTime.Parse(lastRead);

        Assert.Equal(
            expected,
            LogicaApresentacaoBiblioteca.ObterStatusLeitura(current, total, date));
    }

    [Fact]
    public void Status_EpubUsesSameChapterProgressValues()
    {
        var result = Apply(
            format: FiltroFormatoBiblioteca.Epub,
            status: FiltroStatusLeituraBiblioteca.EmAndamento);

        Assert.Single(result);
        Assert.Equal("O Hóbbit", result[0].Title);
    }

    [Theory]
    [InlineData(OpcaoOrdenacaoBiblioteca.ImportadasRecentemente, "Batman - Ano Um")]
    [InlineData(OpcaoOrdenacaoBiblioteca.UltimaLeitura, "O Hóbbit")]
    [InlineData(OpcaoOrdenacaoBiblioteca.TituloCrescente, "Batman - Ano Um")]
    [InlineData(OpcaoOrdenacaoBiblioteca.TituloDecrescente, "O Hóbbit")]
    [InlineData(OpcaoOrdenacaoBiblioteca.ProgressoCrescente, "Batman - Ano Um")]
    [InlineData(OpcaoOrdenacaoBiblioteca.ProgressoDecrescente, "Manual 10")]
    public void Sorting_ReturnsExpectedFirstItem(
        OpcaoOrdenacaoBiblioteca sort,
        string expectedFirst)
    {
        Assert.Equal(expectedFirst, Apply(sort: sort)[0].Title);
    }

    [Fact]
    public void Sorting_TitleUsesNaturalNumberOrder()
    {
        var result = Apply(sort: OpcaoOrdenacaoBiblioteca.TituloCrescente);
        var titles = result.Select(item => item.Title).ToList();

        Assert.True(titles.IndexOf("Manual 2") < titles.IndexOf("Manual 10"));
    }

    [Fact]
    public void Sorting_LastReadPlacesNeverReadLast()
    {
        Assert.Equal("Batman - Ano Um", Apply(sort: OpcaoOrdenacaoBiblioteca.UltimaLeitura)[^1].Title);
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

        var result = Apply(source: tied, sort: OpcaoOrdenacaoBiblioteca.ProgressoCrescente);

        Assert.Equal(["Alfa", "Zulu"], result.Select(item => item.Title));
    }

    [Theory]
    [InlineData(FiltroFormatoBiblioteca.Todos, FiltroStatusLeituraBiblioteca.Todos, OpcaoOrdenacaoBiblioteca.ImportadasRecentemente, 0)]
    [InlineData(FiltroFormatoBiblioteca.Pdf, FiltroStatusLeituraBiblioteca.Todos, OpcaoOrdenacaoBiblioteca.ImportadasRecentemente, 1)]
    [InlineData(FiltroFormatoBiblioteca.Pdf, FiltroStatusLeituraBiblioteca.EmAndamento, OpcaoOrdenacaoBiblioteca.ImportadasRecentemente, 2)]
    [InlineData(FiltroFormatoBiblioteca.Pdf, FiltroStatusLeituraBiblioteca.EmAndamento, OpcaoOrdenacaoBiblioteca.TituloCrescente, 3)]
    public void ActiveFilterCount_ExcludesDefaults(
        FiltroFormatoBiblioteca format,
        FiltroStatusLeituraBiblioteca status,
        OpcaoOrdenacaoBiblioteca sort,
        int expected)
    {
        Assert.Equal(expected, LogicaApresentacaoBiblioteca.ContarFiltrosAtivos(format, status, sort));
    }

    [Fact]
    public void EstadosVazios_DistinguemBibliotecaVaziaDeFiltroSemResultados()
    {
        Assert.True(LogicaApresentacaoBiblioteca.BibliotecaEstaVazia(0));
        Assert.False(LogicaApresentacaoBiblioteca.FiltroEstaVazio(0, 0));
        Assert.True(LogicaApresentacaoBiblioteca.FiltroEstaVazio(4, 0));
    }

    [Fact]
    public void InvalidPersistedSort_ReturnsDefault()
    {
        Assert.Equal(
            OpcaoOrdenacaoBiblioteca.ImportadasRecentemente,
            LogicaApresentacaoBiblioteca.InterpretarOpcaoOrdenacao(999));
    }

    [Fact]
    public void ValidPersistedSort_IsRestored()
    {
        Assert.Equal(
            OpcaoOrdenacaoBiblioteca.TituloDecrescente,
            LogicaApresentacaoBiblioteca.InterpretarOpcaoOrdenacao(
                (int)OpcaoOrdenacaoBiblioteca.TituloDecrescente));
    }

    [Fact]
    public void Pipeline_DoesNotChangeSourceOrder()
    {
        var source = Items.ToList();
        var originalOrder = source.Select(item => item.Title).ToArray();

        _ = Apply(
            query: "manual",
            sort: OpcaoOrdenacaoBiblioteca.TituloDecrescente,
            source: source);

        Assert.Equal(originalOrder, source.Select(item => item.Title));
    }

    private static IReadOnlyList<TestItem> Apply(
        string query = "",
        FiltroFormatoBiblioteca format = FiltroFormatoBiblioteca.Todos,
        FiltroStatusLeituraBiblioteca status = FiltroStatusLeituraBiblioteca.Todos,
        OpcaoOrdenacaoBiblioteca sort = OpcaoOrdenacaoBiblioteca.ImportadasRecentemente,
        IEnumerable<TestItem>? source = null)
    {
        return LogicaApresentacaoBiblioteca.AplicarPipeline(
            source ?? Items,
            new CriteriosFiltroBiblioteca(query, format, status, sort),
            item => item.Title,
            item => item.OriginalFileName,
            item => item.Formato,
            item => item.Current,
            item => item.Total,
            item => item.ImportedAt,
            item => item.LastReadAt);
    }

    private sealed record TestItem(
        string Title,
        string OriginalFileName,
        string Formato,
        int Current,
        int Total,
        DateTime ImportedAt,
        DateTime? LastReadAt);
}
