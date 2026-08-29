using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quadra.App.Data;
using Quadra.App.Models;
using System.Collections.ObjectModel;

namespace Quadra.App.ViewModels;

public sealed record CartaoColecao(Colecao Colecao, IReadOnlyList<ObraBiblioteca> Obras, double Progresso)
{
    public string Quantidade => Obras.Count == 1 ? "1 obra" : $"{Obras.Count} obras";
    public IReadOnlyList<string> Capas => Obras.Select(o => o.CoverPath).Where(p => !string.IsNullOrWhiteSpace(p)).Cast<string>().Take(4).ToList();
    public string? Capa1 => Capas.ElementAtOrDefault(0);
    public string? Capa2 => Capas.ElementAtOrDefault(1);
    public string? Capa3 => Capas.ElementAtOrDefault(2);
    public string? Capa4 => Capas.ElementAtOrDefault(3);
    public bool TemCapa1 => Capa1 is not null;
    public bool TemCapa2 => Capa2 is not null;
    public bool TemCapa3 => Capa3 is not null;
    public bool TemCapa4 => Capa4 is not null;
    public bool SemCapas => Capas.Count == 0;
    public string TextoProgresso => $"{Progresso:P0} concluído";
}

public partial class ColecoesViewModel : ObservableObject
{
    private readonly QuadraDatabase _database;
    public ObservableCollection<CartaoColecao> Colecoes { get; } = [];
    [ObservableProperty] public partial bool EstaCarregando { get; set; }
    public bool Vazia => !EstaCarregando && Colecoes.Count == 0;
    public bool TemColecoes => !EstaCarregando && Colecoes.Count > 0;
    public ColecoesViewModel(QuadraDatabase database) => _database = database;

    partial void OnEstaCarregandoChanged(bool value)
    {
        OnPropertyChanged(nameof(Vazia));
        OnPropertyChanged(nameof(TemColecoes));
    }
    public async Task CarregarAsync()
    {
        EstaCarregando = true;
        try { Colecoes.Clear(); foreach(var colecao in await _database.ObterColecoesAsync()) { var obras=await _database.ObterObrasDaColecaoAsync(colecao.Id); var p=obras.Where(o=>o.TotalPages>0).Select(o=>Math.Clamp((double)o.CurrentPage/Math.Max(1,o.TotalPages-1),0,1)).DefaultIfEmpty(0).Average(); Colecoes.Add(new CartaoColecao(colecao,obras,p)); } }
        finally
        {
            EstaCarregando = false;
            OnPropertyChanged(nameof(Vazia));
            OnPropertyChanged(nameof(TemColecoes));
        }
    }
    public async Task CriarAsync(string nome, string? descricao)
    {
        if(string.IsNullOrWhiteSpace(nome)) return;
        await _database.SalvarColecaoAsync(new Colecao { Nome=nome.Trim(), Descricao=string.IsNullOrWhiteSpace(descricao)?null:descricao.Trim(), DataCriacao=DateTime.Now, Ordem=DateTime.Now.GetHashCode() });
        await CarregarAsync();
    }
    public async Task ExcluirAsync(CartaoColecao cartao) { await _database.ExcluirColecaoAsync(cartao.Colecao); await CarregarAsync(); }
}
