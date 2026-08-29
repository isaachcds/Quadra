using SQLite;

namespace Quadra.App.Models;

[Table("CollectionBooks")]
public sealed class ColecaoObra
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed(Name = "UX_CollectionBooks_CollectionId_BookId", Order = 1)] public int ColecaoId { get; set; }
    [Indexed(Name = "UX_CollectionBooks_CollectionId_BookId", Order = 2)] public int ObraId { get; set; }
    public int Ordem { get; set; }
}
