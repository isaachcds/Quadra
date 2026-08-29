using SQLite;

namespace Quadra.App.Models;

[Table("Collections")]
public sealed class Colecao
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [NotNull] public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataCriacao { get; set; }
    public int Ordem { get; set; }
}
