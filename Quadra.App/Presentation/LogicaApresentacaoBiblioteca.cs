using System.Globalization;
using System.Text;
using Quadra.App.Infrastructure;

namespace Quadra.App.Presentation;

public enum FiltroFormatoBiblioteca
{
    Todos,
    Epub,
    Pdf,
    Quadrinhos
}

public enum FiltroStatusLeituraBiblioteca
{
    Todos,
    NaoIniciada,
    EmAndamento,
    Concluida
}

public enum OpcaoOrdenacaoBiblioteca
{
    ImportadasRecentemente,
    UltimaLeitura,
    TituloCrescente,
    TituloDecrescente,
    ProgressoCrescente,
    ProgressoDecrescente
}

public sealed record CriteriosFiltroBiblioteca(
    string TextoBusca,
    FiltroFormatoBiblioteca Formato,
    FiltroStatusLeituraBiblioteca Status,
    OpcaoOrdenacaoBiblioteca Ordenacao);

public sealed record InformacoesProgressoBiblioteca(
    EstadoProgressoLeitura Estado,
    double Percentual,
    string Texto,
    bool ExibeProgresso);

public static class LogicaApresentacaoBiblioteca
{
    public static IReadOnlyList<T> AplicarPipeline<T>(
        IEnumerable<T> source,
        CriteriosFiltroBiblioteca criteria,
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

        var normalizedSearch = NormalizarTexto(criteria.TextoBusca.Trim());
        IEnumerable<T> result = source;

        if (normalizedSearch.Length > 0)
        {
            result = result.Where(item =>
                ContemNormalizado(titleSelector(item), normalizedSearch) ||
                ContemNormalizado(originalFileNameSelector(item), normalizedSearch) ||
                ContemNormalizado(formatSelector(item), normalizedSearch));
        }

        result = result.Where(item => CorrespondeFormato(
            formatSelector(item),
            criteria.Formato));

        result = result.Where(item => CorrespondeStatus(
            ObterStatusLeitura(
                currentSelector(item),
                totalSelector(item),
                lastReadSelector(item)),
            criteria.Status));

        return Ordenacao(
                result,
                criteria.Ordenacao,
                titleSelector,
                currentSelector,
                totalSelector,
                importedAtSelector,
                lastReadSelector)
            .ToList();
    }

    public static IReadOnlyList<T> Filtrar<T>(
        IEnumerable<T> items,
        FiltroFormatoBiblioteca filter,
        Func<T, string> formatSelector)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(formatSelector);

        return items.Where(item => CorrespondeFormato(formatSelector(item), filter)).ToList();
    }

    public static T? SelecionarContinuarLeitura<T>(
        IEnumerable<T> items,
        Func<T, int> currentSelector,
        Func<T, int> totalSelector,
        Func<T, DateTime?> lastReadSelector)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items
            .Where(item => ObterStatusLeitura(
                    currentSelector(item),
                    totalSelector(item),
                    lastReadSelector(item)) == EstadoProgressoLeitura.EmAndamento &&
                currentSelector(item) > 0)
            .OrderByDescending(lastReadSelector)
            .FirstOrDefault();
    }

    public static InformacoesProgressoBiblioteca CalcularProgresso(
        int currentPosition,
        int totalPositions,
        DateTime? lastReadAt = null)
    {
        var state = ObterStatusLeitura(currentPosition, totalPositions, lastReadAt);

        if (state == EstadoProgressoLeitura.NaoIniciada)
        {
            return new InformacoesProgressoBiblioteca(
                state,
                0,
                "Não iniciado",
                false);
        }

        if (state == EstadoProgressoLeitura.Concluida)
        {
            return new InformacoesProgressoBiblioteca(
                state,
                1,
                "Concluído",
                true);
        }

        var percentage = CalcularPercentual(currentPosition, totalPositions);
        return new InformacoesProgressoBiblioteca(
            state,
            percentage,
            $"{percentage * 100:0}% lido",
            true);
    }

    public static EstadoProgressoLeitura ObterStatusLeitura(
        int currentPosition,
        int totalPositions,
        DateTime? lastReadAt)
    {
        if (!lastReadAt.HasValue || totalPositions <= 0)
            return EstadoProgressoLeitura.NaoIniciada;

        return currentPosition >= totalPositions - 1
            ? EstadoProgressoLeitura.Concluida
            : EstadoProgressoLeitura.EmAndamento;
    }

    public static double CalcularPercentual(int currentPosition, int totalPositions)
    {
        if (totalPositions <= 0)
            return 0;

        return Math.Clamp(
            (double)(currentPosition + 1) / totalPositions,
            0,
            1);
    }

    public static double CalcularPercentualEfetivo(
        int currentPosition,
        int totalPositions,
        DateTime? lastReadAt)
    {
        return ObterStatusLeitura(currentPosition, totalPositions, lastReadAt) switch
        {
            EstadoProgressoLeitura.NaoIniciada => 0,
            EstadoProgressoLeitura.Concluida => 1,
            _ => CalcularPercentual(currentPosition, totalPositions)
        };
    }

    public static int ContarFiltrosAtivos(
        FiltroFormatoBiblioteca format,
        FiltroStatusLeituraBiblioteca status,
        OpcaoOrdenacaoBiblioteca sort)
    {
        var count = 0;
        if (format != FiltroFormatoBiblioteca.Todos)
            count++;
        if (status != FiltroStatusLeituraBiblioteca.Todos)
            count++;
        if (sort != OpcaoOrdenacaoBiblioteca.ImportadasRecentemente)
            count++;
        return count;
    }

    public static bool BibliotecaEstaVazia(int sourceCount) => sourceCount == 0;

    public static bool FiltroEstaVazio(int sourceCount, int visibleCount)
    {
        return sourceCount > 0 && visibleCount == 0;
    }

    public static OpcaoOrdenacaoBiblioteca InterpretarOpcaoOrdenacao(int value)
    {
        return Enum.IsDefined(typeof(OpcaoOrdenacaoBiblioteca), value)
            ? (OpcaoOrdenacaoBiblioteca)value
            : OpcaoOrdenacaoBiblioteca.ImportadasRecentemente;
    }

    public static string NormalizarTexto(string? value)
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

    private static IEnumerable<T> Ordenacao<T>(
        IEnumerable<T> items,
        OpcaoOrdenacaoBiblioteca sort,
        Func<T, string> titleSelector,
        Func<T, int> currentSelector,
        Func<T, int> totalSelector,
        Func<T, DateTime> importedAtSelector,
        Func<T, DateTime?> lastReadSelector)
    {
        var titleComparer = ComparadorNaturalNormalizado.Instance;

        return sort switch
        {
            OpcaoOrdenacaoBiblioteca.UltimaLeitura => items
                .OrderBy(item => lastReadSelector(item).HasValue ? 0 : 1)
                .ThenByDescending(lastReadSelector)
                .ThenByDescending(importedAtSelector)
                .ThenBy(titleSelector, titleComparer),
            OpcaoOrdenacaoBiblioteca.TituloCrescente => items
                .OrderBy(titleSelector, titleComparer)
                .ThenByDescending(importedAtSelector),
            OpcaoOrdenacaoBiblioteca.TituloDecrescente => items
                .OrderByDescending(titleSelector, titleComparer)
                .ThenByDescending(importedAtSelector),
            OpcaoOrdenacaoBiblioteca.ProgressoCrescente => items
                .OrderBy(item => CalcularPercentualEfetivo(
                    currentSelector(item),
                    totalSelector(item),
                    lastReadSelector(item)))
                .ThenBy(titleSelector, titleComparer),
            OpcaoOrdenacaoBiblioteca.ProgressoDecrescente => items
                .OrderByDescending(item => CalcularPercentualEfetivo(
                    currentSelector(item),
                    totalSelector(item),
                    lastReadSelector(item)))
                .ThenBy(titleSelector, titleComparer),
            _ => items
                .OrderByDescending(importedAtSelector)
                .ThenBy(titleSelector, titleComparer)
        };
    }

    private static bool ContemNormalizado(string? value, string normalizedSearch)
    {
        return NormalizarTexto(value).Contains(normalizedSearch, StringComparison.Ordinal);
    }

    private static bool CorrespondeStatus(
        EstadoProgressoLeitura state,
        FiltroStatusLeituraBiblioteca filter)
    {
        return filter switch
        {
            FiltroStatusLeituraBiblioteca.Todos => true,
            FiltroStatusLeituraBiblioteca.NaoIniciada => state == EstadoProgressoLeitura.NaoIniciada,
            FiltroStatusLeituraBiblioteca.EmAndamento => state == EstadoProgressoLeitura.EmAndamento,
            FiltroStatusLeituraBiblioteca.Concluida => state == EstadoProgressoLeitura.Concluida,
            _ => false
        };
    }

    private static bool CorrespondeFormato(string format, FiltroFormatoBiblioteca filter)
    {
        return filter switch
        {
            FiltroFormatoBiblioteca.Todos => true,
            FiltroFormatoBiblioteca.Epub => format.Equals("EPUB", StringComparison.OrdinalIgnoreCase),
            FiltroFormatoBiblioteca.Pdf => format.Equals("PDF", StringComparison.OrdinalIgnoreCase),
            FiltroFormatoBiblioteca.Quadrinhos =>
                format.Equals("CBR", StringComparison.OrdinalIgnoreCase) ||
                format.Equals("CBZ", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private sealed class ComparadorNaturalNormalizado : IComparer<string>
    {
        public static ComparadorNaturalNormalizado Instance { get; } = new();

        public int Compare(string? x, string? y)
        {
            return NaturalStringComparer.Instance.Compare(
                NormalizarTexto(x),
                NormalizarTexto(y));
        }
    }
}
