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
            () => _connection.CreateTableAsync<ObraBiblioteca>());
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

        return await _connection.DeleteAsync(item);
    }
}
