using Quadra.App.Models;
using Quadra.App.Presentation;

namespace Quadra.App.ViewModels;

public sealed class DadosObraBiblioteca
{
    public DadosObraBiblioteca(ObraBiblioteca item)
    {
        Item = item;
        Progresso = LogicaApresentacaoBiblioteca.CalcularProgresso(
            item.CurrentPage,
            item.TotalPages,
            item.LastReadAt);
    }

    public ObraBiblioteca Item { get; }
    public InformacoesProgressoBiblioteca Progresso { get; }
    public string Titulo => Item.Title;
    public string Formato => Item.Format.ToUpperInvariant();
    public string? CaminhoCapa => Item.CoverPath;
    public bool PossuiCapa => !string.IsNullOrWhiteSpace(CaminhoCapa) &&
                              File.Exists(CaminhoCapa);
    public double ValorProgresso => Progresso.Percentual;
    public string TextoProgresso => Progresso.Texto;
    public bool ExibeProgresso => Progresso.ExibeProgresso;
    public bool Concluida => Progresso.Estado == EstadoProgressoLeitura.Concluida;
    public string DescricaoCapa => $"Capa de {Titulo}, formato {Formato}";
}
