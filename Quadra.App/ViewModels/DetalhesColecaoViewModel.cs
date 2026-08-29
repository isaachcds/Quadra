using CommunityToolkit.Mvvm.ComponentModel;
using Quadra.App.Data;
using Quadra.App.Models;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public partial class DetalhesColecaoViewModel : ObservableObject, IQueryAttributable
{
    private readonly QuadraDatabase _database;
    [ObservableProperty] public partial Colecao? Colecao { get; set; }
    public ObservableCollection<ObraBiblioteca> Obras { get; } = [];
    public ObservableCollection<ObraBiblioteca> ObrasDisponiveis { get; } = [];
    public int Quantidade => Obras.Count;
    public double Progresso => Obras.Where(o => o.TotalPages > 0).Select(o => Math.Clamp((double)o.CurrentPage / Math.Max(1,o.TotalPages-1),0,1)).DefaultIfEmpty(0).Average();
    public DetalhesColecaoViewModel(QuadraDatabase database) => _database=database;
    public async void ApplyQueryAttributes(IDictionary<string,object> query) { if(query.TryGetValue("Colecao",out var valor) && valor is Colecao colecao) { Colecao=colecao; await CarregarAsync(); } }
    public async Task CarregarAsync() { if(Colecao is null)return; Obras.Clear(); foreach(var obra in await _database.ObterObrasDaColecaoAsync(Colecao.Id)) Obras.Add(obra); ObrasDisponiveis.Clear(); foreach(var obra in (await _database.ObterObrasBibliotecaAsync()).Where(o=>Obras.All(atual=>atual.Id!=o.Id))) ObrasDisponiveis.Add(obra); OnPropertyChanged(nameof(Quantidade));OnPropertyChanged(nameof(Progresso)); }
    public async Task SalvarAsync(string nome,string? descricao) { if(Colecao is null||string.IsNullOrWhiteSpace(nome))return; Colecao.Nome=nome.Trim();Colecao.Descricao=string.IsNullOrWhiteSpace(descricao)?null:descricao.Trim();await _database.SalvarColecaoAsync(Colecao);OnPropertyChanged(nameof(Colecao)); }
    public async Task RemoverObraAsync(ObraBiblioteca obra) { if(Colecao is null)return;await _database.DefinirObraNaColecaoAsync(Colecao.Id,obra.Id,false);await CarregarAsync(); }
    public async Task AdicionarObraAsync(ObraBiblioteca obra) { if(Colecao is null)return; await _database.DefinirObraNaColecaoAsync(Colecao.Id,obra.Id,true);await CarregarAsync(); }
    public async Task ExcluirAsync() { if(Colecao is not null)await _database.ExcluirColecaoAsync(Colecao); }
}
