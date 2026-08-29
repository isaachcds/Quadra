using CommunityToolkit.Mvvm.ComponentModel;
using Quadra.App.Data;
using Quadra.App.Models;
using Quadra.App.Presentation;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;
public sealed record ItemHistorico(ObraBiblioteca Obra, string UltimaLeitura, ProgressoDetalhesObra Progresso)
{
    public string Formato => Obra.Format.ToUpperInvariant();
    public string Posicao => Progresso.TextoPosicao;
    public string TextoProgresso => $"{Progresso.TextoStatus} · {Progresso.TextoPosicao}";
    public string TextoAcaoLeitura => Progresso.TextoBotao;
    public bool PossuiCapa => !string.IsNullOrWhiteSpace(Obra.CoverPath) && File.Exists(Obra.CoverPath);
    public string DescricaoCapa => $"Capa de {Obra.Title}, formato {Formato}";
}
public partial class HistoricoViewModel:ObservableObject
{ readonly QuadraDatabase _database; public ObservableCollection<ItemHistorico> Itens {get;}=[]; [ObservableProperty] public partial bool Vazio {get;set;}
 public HistoricoViewModel(QuadraDatabase database)=>_database=database;
 public async Task CarregarAsync(){Itens.Clear();foreach(var obra in (await _database.ObterObrasBibliotecaAsync()).Where(o=>o.LastReadAt.HasValue).OrderByDescending(o=>o.LastReadAt)){var p=ApresentacaoDetalhesObra.CalcularProgresso(obra.Format,obra.CurrentPage,obra.TotalPages,true);Itens.Add(new ItemHistorico(obra,obra.LastReadAt!.Value.ToString("dd/MM/yyyy 'às' HH:mm"),p));}Vazio=Itens.Count==0;}
}
