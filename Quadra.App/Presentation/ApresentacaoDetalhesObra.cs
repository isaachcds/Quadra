using System.Globalization;

namespace Quadra.App.Presentation;

public sealed record ProgressoDetalhesObra(
    EstadoProgressoLeitura Estado,
    double Percentual,
    string TextoStatus,
    string TextoPosicao,
    string TextoBotao);

public static class ApresentacaoDetalhesObra
{
    public static bool ArquivoDisponivel(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    public static UnidadeProgressoLeitura ObterUnidadeProgresso(string? format)
    {
        return string.Equals(format, "EPUB", StringComparison.OrdinalIgnoreCase)
            ? UnidadeProgressoLeitura.Capitulo
            : UnidadeProgressoLeitura.Pagina;
    }

    public static string FormatarTotal(string? format, int total)
    {
        var safeTotal = Math.Max(0, total);
        var unit = ObterUnidadeProgresso(format);
        var label = unit == UnidadeProgressoLeitura.Capitulo
            ? safeTotal == 1 ? "capítulo" : "capítulos"
            : safeTotal == 1 ? "página" : "páginas";

        return $"{safeTotal} {label}";
    }

    public static ProgressoDetalhesObra CalcularProgresso(
        string? format,
        int currentPosition,
        int totalPositions,
        bool hasBeenRead)
    {
        var state = LogicaApresentacaoBiblioteca.ObterStatusLeitura(
            currentPosition,
            totalPositions,
            hasBeenRead ? DateTime.MinValue : null);

        if (state == EstadoProgressoLeitura.NaoIniciada)
        {
            return new ProgressoDetalhesObra(
                EstadoProgressoLeitura.NaoIniciada,
                0,
                "Não iniciado",
                FormatarTotal(format, totalPositions),
                "Começar leitura");
        }

        var progress = CalculadoraProgressoLeitura.Calcular(
            currentPosition,
            totalPositions,
            ObterUnidadeProgresso(format));

        var status = state == EstadoProgressoLeitura.Concluida
            ? "Concluído"
            : "Em andamento";

        return new ProgressoDetalhesObra(
            progress.Estado,
            Math.Clamp(progress.Percentual, 0, 1),
            status,
            progress.Texto,
            progress.TextoBotao);
    }

    public static string FormatarTamanhoArquivo(long? bytes)
    {
        if (bytes is null || bytes < 0)
            return "Indisponível";

        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes.Value;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        var format = unitIndex == 0 ? "0" : "0.#";
        return $"{value.ToString(format, CultureInfo.CurrentCulture)} {units[unitIndex]}";
    }
}
