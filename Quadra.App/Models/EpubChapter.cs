namespace Quadra.App.Models;

public class EpubChapter
{
    public int Index { get; set; }

    public string Title { get; set; } = string.Empty;

    public string OriginalPath { get; set; } = string.Empty;

    public string LocalFilePath { get; set; } = string.Empty;
}