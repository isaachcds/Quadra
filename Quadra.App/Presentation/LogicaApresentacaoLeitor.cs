namespace Quadra.App.Presentation;

public enum AcaoToqueLeitor
{
    Nenhuma,
    Anterior,
    Proxima,
    AlternarControles
}

public readonly record struct EstadoPaginaLeitor(
    int Indice,
    int IndiceMaximo,
    string TextoContador,
    bool PodeVoltar,
    bool PodeAvancar,
    double Progresso);

public static class LogicaApresentacaoLeitor
{
    public static EstadoPaginaLeitor CriarEstadoPagina(int requestedIndex, int totalPages)
    {
        if (totalPages <= 0)
            return new EstadoPaginaLeitor(0, 0, string.Empty, false, false, 0);

        var index = Math.Clamp(requestedIndex, 0, totalPages - 1);
        return new EstadoPaginaLeitor(
            index,
            totalPages - 1,
            $"{index + 1} / {totalPages}",
            index > 0,
            index < totalPages - 1,
            Math.Clamp((double)(index + 1) / totalPages, 0, 1));
    }

    public static AcaoToqueLeitor DecidirToque(
        double horizontalRatio,
        bool edgeTapEnabled,
        bool isZoomed,
        bool isPanning,
        bool isSwiping,
        bool isDoubleTap)
    {
        if (isZoomed || isPanning || isSwiping || isDoubleTap)
            return AcaoToqueLeitor.Nenhuma;

        var ratio = Math.Clamp(horizontalRatio, 0, 1);
        if (ratio < 0.30)
            return edgeTapEnabled ? AcaoToqueLeitor.Anterior : AcaoToqueLeitor.Nenhuma;
        if (ratio > 0.70)
            return edgeTapEnabled ? AcaoToqueLeitor.Proxima : AcaoToqueLeitor.Nenhuma;

        return AcaoToqueLeitor.AlternarControles;
    }
}

public sealed class EstadoFocoLeitor
{
    public bool ControlesVisiveis { get; private set; } = true;
    public bool ConfiguracoesVisiveis { get; private set; }

    public void AlternarControles()
    {
        if (ConfiguracoesVisiveis)
            return;

        ControlesVisiveis = !ControlesVisiveis;
    }

    public void ExibirControles() => ControlesVisiveis = true;

    public void OcultarControles()
    {
        if (!ConfiguracoesVisiveis)
            ControlesVisiveis = false;
    }

    public void AbrirConfiguracoes()
    {
        ConfiguracoesVisiveis = true;
        ControlesVisiveis = true;
    }

    public void FecharConfiguracoes()
    {
        ConfiguracoesVisiveis = false;
        ControlesVisiveis = true;
    }
}

public sealed class CoordenadorFechamentoLeitor
{
    private readonly object _gate = new();
    private Task? _closeTask;

    public Task FecharAsync(
        Func<Task> flushProgress,
        Action cancelLoading,
        Action cancelAutoHide)
    {
        ArgumentNullException.ThrowIfNull(flushProgress);
        ArgumentNullException.ThrowIfNull(cancelLoading);
        ArgumentNullException.ThrowIfNull(cancelAutoHide);

        lock (_gate)
        {
            return _closeTask ??= FecharNucleoAsync(
                flushProgress,
                cancelLoading,
                cancelAutoHide);
        }
    }

    private static async Task FecharNucleoAsync(
        Func<Task> flushProgress,
        Action cancelLoading,
        Action cancelAutoHide)
    {
        cancelAutoHide();
        cancelLoading();
        await flushProgress();
    }
}
