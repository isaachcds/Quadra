using Quadra.App.Models;
using Quadra.App.Infrastructure;
using SQLite;

namespace Quadra.App.Data;

public class QuadraDatabase
{
    private readonly SQLiteAsyncConnection _connection;
    private readonly AsyncInitializationGate
        _initializationGate = new();

    public QuadraDatabase()
    {
        var databasePath = Path.Combine(
            FileSystem.Current.AppDataDirectory,
            "quadra.db3");

        const SQLiteOpenFlags flags =
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache;

        _connection = new SQLiteAsyncConnection(
            databasePath,
            flags);
    }

    private async Task InitializeAsync()
    {
        await _initializationGate.EnsureInitializedAsync(
            async () =>
            {
                await _connection.CreateTableAsync<ObraBiblioteca>();
                await _connection.CreateTableAsync<Colecao>();
                await _connection.CreateTableAsync<ColecaoObra>();
            });
    }

    public async Task<List<ObraBiblioteca>> ObterObrasBibliotecaAsync()
    {
        await InitializeAsync();

        return await _connection
            .Table<ObraBiblioteca>()
            .OrderByDescending(item => item.ImportedAt)
            .ToListAsync();
    }

    public async Task<ObraBiblioteca?> ObterObraBibliotecaAsync(int id)
    {
        await InitializeAsync();

        return await _connection
            .Table<ObraBiblioteca>()
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<int> SalvarObraBibliotecaAsync(ObraBiblioteca item)
    {
        ArgumentNullException.ThrowIfNull(item);

        await InitializeAsync();

        if (item.Id != 0)
            return await _connection.UpdateAsync(item);

        return await _connection.InsertAsync(item);
    }

    public async Task<int> ExcluirObraBibliotecaAsync(ObraBiblioteca item)
    {
        ArgumentNullException.ThrowIfNull(item);

        await InitializeAsync();

        await _connection.Table<ColecaoObra>().DeleteAsync(relacao => relacao.ObraId == item.Id);
        return await _connection.DeleteAsync(item);
    }

    public async Task<List<Colecao>> ObterColecoesAsync()
    {
        await InitializeAsync();
        return await _connection.Table<Colecao>().OrderBy(c => c.Ordem).ThenBy(c => c.Nome).ToListAsync();
    }

    public async Task<int> SalvarColecaoAsync(Colecao colecao)
    {
        await InitializeAsync();
        return colecao.Id == 0 ? await _connection.InsertAsync(colecao) : await _connection.UpdateAsync(colecao);
    }

    public async Task ExcluirColecaoAsync(Colecao colecao)
    {
        await InitializeAsync();
        await _connection.RunInTransactionAsync(connection =>
        {
            connection.Table<ColecaoObra>().Delete(relacao => relacao.ColecaoId == colecao.Id);
            connection.Delete(colecao);
        });
    }

    public async Task<List<ObraBiblioteca>> ObterObrasDaColecaoAsync(int colecaoId)
    {
        await InitializeAsync();
        return await _connection.QueryAsync<ObraBiblioteca>("SELECT b.* FROM LibraryItems b INNER JOIN CollectionBooks cb ON cb.ObraId = b.Id WHERE cb.ColecaoId = ? ORDER BY cb.Ordem, b.Title", colecaoId);
    }

    public async Task<List<Colecao>> ObterColecoesDaObraAsync(int obraId)
    {
        await InitializeAsync();
        return await _connection.QueryAsync<Colecao>(
            "SELECT c.* FROM Collections c INNER JOIN CollectionBooks cb ON cb.ColecaoId = c.Id WHERE cb.ObraId = ? ORDER BY c.Ordem, c.Nome",
            obraId);
    }

    public async Task DefinirObraNaColecaoAsync(int colecaoId, int obraId, bool incluir)
    {
        await InitializeAsync();
        if (!incluir) { await _connection.Table<ColecaoObra>().DeleteAsync(relacao => relacao.ColecaoId == colecaoId && relacao.ObraId == obraId); return; }
        var existe = await _connection.Table<ColecaoObra>().Where(relacao => relacao.ColecaoId == colecaoId && relacao.ObraId == obraId).FirstOrDefaultAsync();
        if (existe is null) await _connection.InsertAsync(new ColecaoObra { ColecaoId = colecaoId, ObraId = obraId });
    }
}
