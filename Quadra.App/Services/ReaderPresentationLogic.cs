namespace Quadra.App.Services;

public enum ReaderTapAction
{
    None,
    Previous,
    Next,
    ToggleControls
}

public readonly record struct ReaderPageState(
    int Index,
    int MaximumIndex,
    string CounterText,
    bool CanGoPrevious,
    bool CanGoNext,
    double Progress);

public static class ReaderPresentationLogic
{
    public static ReaderPageState CreatePageState(int requestedIndex, int totalPages)
    {
        if (totalPages <= 0)
            return new ReaderPageState(0, 0, string.Empty, false, false, 0);

        var index = Math.Clamp(requestedIndex, 0, totalPages - 1);
        return new ReaderPageState(
            index,
            totalPages - 1,
            $"{index + 1} / {totalPages}",
            index > 0,
            index < totalPages - 1,
            Math.Clamp((double)(index + 1) / totalPages, 0, 1));
    }

    public static ReaderTapAction DecideTap(
        double horizontalRatio,
        bool edgeTapEnabled,
        bool isZoomed,
        bool isPanning,
        bool isSwiping,
        bool isDoubleTap)
    {
        if (isZoomed || isPanning || isSwiping || isDoubleTap)
            return ReaderTapAction.None;

        var ratio = Math.Clamp(horizontalRatio, 0, 1);
        if (ratio < 0.30)
            return edgeTapEnabled ? ReaderTapAction.Previous : ReaderTapAction.None;
        if (ratio > 0.70)
            return edgeTapEnabled ? ReaderTapAction.Next : ReaderTapAction.None;

        return ReaderTapAction.ToggleControls;
    }
}

public sealed class ReaderFocusState
{
    public bool ControlsVisible { get; private set; } = true;
    public bool SettingsVisible { get; private set; }

    public void ToggleControls()
    {
        if (SettingsVisible)
            return;

        ControlsVisible = !ControlsVisible;
    }

    public void ShowControls() => ControlsVisible = true;

    public void HideControls()
    {
        if (!SettingsVisible)
            ControlsVisible = false;
    }

    public void OpenSettings()
    {
        SettingsVisible = true;
        ControlsVisible = true;
    }

    public void CloseSettings()
    {
        SettingsVisible = false;
        ControlsVisible = true;
    }
}

public sealed class ReaderCloseCoordinator
{
    private readonly object _gate = new();
    private Task? _closeTask;

    public Task CloseAsync(
        Func<Task> flushProgress,
        Action cancelLoading,
        Action cancelAutoHide)
    {
        ArgumentNullException.ThrowIfNull(flushProgress);
        ArgumentNullException.ThrowIfNull(cancelLoading);
        ArgumentNullException.ThrowIfNull(cancelAutoHide);

        lock (_gate)
        {
            return _closeTask ??= CloseCoreAsync(
                flushProgress,
                cancelLoading,
                cancelAutoHide);
        }
    }

    private static async Task CloseCoreAsync(
        Func<Task> flushProgress,
        Action cancelLoading,
        Action cancelAutoHide)
    {
        cancelAutoHide();
        cancelLoading();
        await flushProgress();
    }
}
