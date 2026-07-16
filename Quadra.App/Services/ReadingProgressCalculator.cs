namespace Quadra.App.Services;

public enum ReadingProgressState
{
    NotStarted,
    InProgress,
    Completed
}

public enum ReadingProgressUnit
{
    Page,
    Chapter
}

public sealed record ReadingProgressResult(
    ReadingProgressState State,
    int DisplayedPosition,
    double Percentage,
    string Text,
    string ButtonText);

public static class ReadingProgressCalculator
{
    public static ReadingProgressResult Calculate(
        int currentPosition,
        int totalPositions,
        ReadingProgressUnit unit)
    {
        if (totalPositions <= 0)
        {
            return new ReadingProgressResult(
                ReadingProgressState.NotStarted,
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
            return new ReadingProgressResult(
                ReadingProgressState.Completed,
                displayedPosition,
                percentage,
                "Leitura concluída",
                "Ler novamente");
        }

        var label = unit == ReadingProgressUnit.Chapter
            ? "Capítulo"
            : "Página";

        return new ReadingProgressResult(
            ReadingProgressState.InProgress,
            displayedPosition,
            percentage,
            $"{label} {displayedPosition} de {totalPositions}",
            currentPosition > 0
                ? "Continuar leitura"
                : "Começar leitura");
    }
}
