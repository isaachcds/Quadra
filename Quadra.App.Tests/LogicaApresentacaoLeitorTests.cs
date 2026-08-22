using Quadra.App.Presentation;

namespace Quadra.App.Tests;

public sealed class LogicaApresentacaoLeitorTests
{
    [Theory]
    [InlineData(0, 5, "1 / 5")]
    [InlineData(2, 5, "3 / 5")]
    [InlineData(4, 5, "5 / 5")]
    [InlineData(0, 1, "1 / 1")]
    [InlineData(-9, 5, "1 / 5")]
    [InlineData(99, 5, "5 / 5")]
    public void PageState_FormatsCounterAndClampsIndex(
        int index,
        int total,
        string expected)
    {
        var state = LogicaApresentacaoLeitor.CriarEstadoPagina(index, total);

        Assert.Equal(expected, state.TextoContador);
        Assert.InRange(state.Indice, 0, Math.Max(0, total - 1));
    }

    [Theory]
    [InlineData(0, 5, false, true)]
    [InlineData(2, 5, true, true)]
    [InlineData(4, 5, true, false)]
    [InlineData(0, 1, false, false)]
    public void PageState_ReportsNavigationAvailability(
        int index,
        int total,
        bool previous,
        bool next)
    {
        var state = LogicaApresentacaoLeitor.CriarEstadoPagina(index, total);

        Assert.Equal(previous, state.PodeVoltar);
        Assert.Equal(next, state.PodeAvancar);
    }

    [Theory]
    [InlineData(-4, 10, 0, 9, 0.1)]
    [InlineData(4, 10, 4, 9, 0.5)]
    [InlineData(40, 10, 9, 9, 1)]
    public void PageState_LimitsSliderAndProgress(
        int requested,
        int total,
        int index,
        int maximum,
        double progress)
    {
        var state = LogicaApresentacaoLeitor.CriarEstadoPagina(requested, total);

        Assert.Equal(index, state.Indice);
        Assert.Equal(maximum, state.IndiceMaximo);
        Assert.Equal(progress, state.Progresso);
        Assert.InRange(state.Progresso, 0, 1);
    }

    [Fact]
    public void FocusState_StartsVisibleAndToggles()
    {
        var state = new EstadoFocoLeitor();

        Assert.True(state.ControlesVisiveis);
        state.AlternarControles();
        Assert.False(state.ControlesVisiveis);
        state.AlternarControles();
        Assert.True(state.ControlesVisiveis);
    }

    [Fact]
    public void FocusState_SettingsKeepControlsVisible()
    {
        var state = new EstadoFocoLeitor();
        state.AbrirConfiguracoes();
        state.AlternarControles();

        Assert.True(state.ConfiguracoesVisiveis);
        Assert.True(state.ControlesVisiveis);

        state.FecharConfiguracoes();
        Assert.False(state.ConfiguracoesVisiveis);
        Assert.True(state.ControlesVisiveis);
    }

    [Theory]
    [InlineData(0.1, false, AcaoToqueLeitor.Nenhuma)]
    [InlineData(0.1, true, AcaoToqueLeitor.Anterior)]
    [InlineData(0.5, false, AcaoToqueLeitor.AlternarControles)]
    [InlineData(0.5, true, AcaoToqueLeitor.AlternarControles)]
    [InlineData(0.9, false, AcaoToqueLeitor.Nenhuma)]
    [InlineData(0.9, true, AcaoToqueLeitor.Proxima)]
    public void TapDecision_RespectsNavigationPreference(
        double ratio,
        bool edgeTap,
        AcaoToqueLeitor expected)
    {
        Assert.Equal(
            expected,
            LogicaApresentacaoLeitor.DecidirToque(
                ratio, edgeTap, false, false, false, false));
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, false, true)]
    public void TapDecision_BlocksWhileImageOrGestureOwnsInteraction(
        bool zoomed,
        bool panning,
        bool swiping,
        bool doubleTap)
    {
        Assert.Equal(
            AcaoToqueLeitor.Nenhuma,
            LogicaApresentacaoLeitor.DecidirToque(
                0.9, true, zoomed, panning, swiping, doubleTap));
    }

    [Fact]
    public async Task CloseCoordinator_IsIdempotentAndFlushesOnce()
    {
        var coordinator = new CoordenadorFechamentoLeitor();
        var flushCount = 0;
        var loadingCancelCount = 0;
        var autoHideCancelCount = 0;

        Task Flush()
        {
            Interlocked.Increment(ref flushCount);
            return Task.CompletedTask;
        }

        await Task.WhenAll(
            coordinator.FecharAsync(
                Flush,
                () => Interlocked.Increment(ref loadingCancelCount),
                () => Interlocked.Increment(ref autoHideCancelCount)),
            coordinator.FecharAsync(
                Flush,
                () => Interlocked.Increment(ref loadingCancelCount),
                () => Interlocked.Increment(ref autoHideCancelCount)));

        Assert.Equal(1, flushCount);
        Assert.Equal(1, loadingCancelCount);
        Assert.Equal(1, autoHideCancelCount);
    }
}
