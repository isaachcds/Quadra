namespace Quadra.App.Presentation;

public enum EstadoProgressoLeitura
{
    NaoIniciada,
    EmAndamento,
    Concluida
}

public enum UnidadeProgressoLeitura
{
    Pagina,
    Capitulo
}

public sealed record ResultadoProgressoLeitura(
    EstadoProgressoLeitura Estado,
    int PosicaoExibida,
    double Percentual,
    string Texto,
    string TextoBotao);

public static class CalculadoraProgressoLeitura
{
    public static ResultadoProgressoLeitura Calcular(
        int currentPosition,
        int totalPositions,
        UnidadeProgressoLeitura unit)
    {
        if (totalPositions <= 0)
        {
            return new ResultadoProgressoLeitura(
                EstadoProgressoLeitura.NaoIniciada,
                0,
                0,
                "Ainda não iniciado",
                "Começar leitura");
        }

        var displayedPosition = Math.Clamp(
            currentPosition + 1,
            1,
            totalPositions);

        var completed = currentPosition >= totalPositions - 1;
        var percentage = Math.Clamp(
            (double)displayedPosition / totalPositions,
            0,
            1);

        if (completed)
        {
            return new ResultadoProgressoLeitura(
                EstadoProgressoLeitura.Concluida,
                displayedPosition,
                percentage,
                "Leitura concluída",
                "Ler novamente");
        }

        var label = unit == UnidadeProgressoLeitura.Capitulo
            ? "Capítulo"
            : "Página";

        return new ResultadoProgressoLeitura(
            EstadoProgressoLeitura.EmAndamento,
            displayedPosition,
            percentage,
            $"{label} {displayedPosition} de {totalPositions}",
            currentPosition > 0
                ? "Continuar leitura"
                : "Começar leitura");
    }
}
