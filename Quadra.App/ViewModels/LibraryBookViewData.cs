using Quadra.App.Models;
using Quadra.App.Services;

namespace Quadra.App.ViewModels;

public sealed class LibraryBookViewData
{
    public LibraryBookViewData(LibraryItem item)
    {
        Item = item;
        Progress = LibraryPresentationLogic.CalculateProgress(
            item.CurrentPage,
            item.TotalPages);
    }

    public LibraryItem Item { get; }
    public LibraryProgressInfo Progress { get; }
    public string Title => Item.Title;
    public string Format => Item.Format.ToUpperInvariant();
    public string? CoverPath => Item.CoverPath;
    public bool HasCover => !string.IsNullOrWhiteSpace(CoverPath) &&
                            File.Exists(CoverPath);
    public double ProgressValue => Progress.Percentage;
    public string ProgressText => Progress.Text;
    public bool ShowsProgress => Progress.ShowsProgress;
    public bool IsCompleted => Progress.State == ReadingProgressState.Completed;
    public string CoverDescription => $"Capa de {Title}, formato {Format}";
}
