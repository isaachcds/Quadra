using Quadra.App.Services;

namespace Quadra.App.Tests;

public sealed class ReaderPresentationLogicTests
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
        var state = ReaderPresentationLogic.CreatePageState(index, total);

        Assert.Equal(expected, state.CounterText);
        Assert.InRange(state.Index, 0, Math.Max(0, total - 1));
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
        var state = ReaderPresentationLogic.CreatePageState(index, total);

        Assert.Equal(previous, state.CanGoPrevious);
        Assert.Equal(next, state.CanGoNext);
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
        var state = ReaderPresentationLogic.CreatePageState(requested, total);

        Assert.Equal(index, state.Index);
        Assert.Equal(maximum, state.MaximumIndex);
        Assert.Equal(progress, state.Progress);
        Assert.InRange(state.Progress, 0, 1);
    }

    [Fact]
    public void FocusState_StartsVisibleAndToggles()
    {
        var state = new ReaderFocusState();

        Assert.True(state.ControlsVisible);
        state.ToggleControls();
        Assert.False(state.ControlsVisible);
        state.ToggleControls();
        Assert.True(state.ControlsVisible);
    }

    [Fact]
    public void FocusState_SettingsKeepControlsVisible()
    {
        var state = new ReaderFocusState();
        state.OpenSettings();
        state.ToggleControls();

        Assert.True(state.SettingsVisible);
        Assert.True(state.ControlsVisible);

        state.CloseSettings();
        Assert.False(state.SettingsVisible);
        Assert.True(state.ControlsVisible);
    }

    [Theory]
    [InlineData(0.1, false, ReaderTapAction.None)]
    [InlineData(0.1, true, ReaderTapAction.Previous)]
    [InlineData(0.5, false, ReaderTapAction.ToggleControls)]
    [InlineData(0.5, true, ReaderTapAction.ToggleControls)]
    [InlineData(0.9, false, ReaderTapAction.None)]
    [InlineData(0.9, true, ReaderTapAction.Next)]
    public void TapDecision_RespectsNavigationPreference(
        double ratio,
        bool edgeTap,
        ReaderTapAction expected)
    {
        Assert.Equal(
            expected,
            ReaderPresentationLogic.DecideTap(
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
            ReaderTapAction.None,
            ReaderPresentationLogic.DecideTap(
                0.9, true, zoomed, panning, swiping, doubleTap));
    }

    [Fact]
    public async Task CloseCoordinator_IsIdempotentAndFlushesOnce()
    {
        var coordinator = new ReaderCloseCoordinator();
        var flushCount = 0;
        var loadingCancelCount = 0;
        var autoHideCancelCount = 0;

        Task Flush()
        {
            Interlocked.Increment(ref flushCount);
            return Task.CompletedTask;
        }

        await Task.WhenAll(
            coordinator.CloseAsync(
                Flush,
                () => Interlocked.Increment(ref loadingCancelCount),
                () => Interlocked.Increment(ref autoHideCancelCount)),
            coordinator.CloseAsync(
                Flush,
                () => Interlocked.Increment(ref loadingCancelCount),
                () => Interlocked.Increment(ref autoHideCancelCount)));

        Assert.Equal(1, flushCount);
        Assert.Equal(1, loadingCancelCount);
        Assert.Equal(1, autoHideCancelCount);
    }
}
