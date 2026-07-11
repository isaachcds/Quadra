using SQLite;

namespace Quadra.App.Models;

[Table("LibraryItems")]
public class LibraryItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Title { get; set; } = string.Empty;

    [NotNull]
    public string OriginalFileName { get; set; } = string.Empty;

    [NotNull]
    public string StoredFileName { get; set; } = string.Empty;

    [NotNull]
    public string FilePath { get; set; } = string.Empty;

    [NotNull]
    public string Format { get; set; } = string.Empty;

    public string? CoverPath { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public DateTime ImportedAt { get; set; }

    public DateTime? LastReadAt { get; set; }
}