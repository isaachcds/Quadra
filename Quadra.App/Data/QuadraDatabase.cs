using Quadra.App.Models;
using SQLite;

namespace Quadra.App.Data;

public class QuadraDatabase
{
    private readonly SQLiteAsyncConnection _connection;
    private bool _initialized;

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
        if (_initialized)
            return;

        await _connection.CreateTableAsync<LibraryItem>();

        _initialized = true;
    }

    public async Task<List<LibraryItem>> GetLibraryItemsAsync()
    {
        await InitializeAsync();

        return await _connection
            .Table<LibraryItem>()
            .OrderByDescending(item => item.ImportedAt)
            .ToListAsync();
    }

    public async Task<LibraryItem?> GetLibraryItemAsync(int id)
    {
        await InitializeAsync();

        return await _connection
            .Table<LibraryItem>()
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public async Task<int> SaveLibraryItemAsync(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        await InitializeAsync();

        if (item.Id != 0)
            return await _connection.UpdateAsync(item);

        return await _connection.InsertAsync(item);
    }

    public async Task<int> DeleteLibraryItemAsync(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        await InitializeAsync();

        return await _connection.DeleteAsync(item);
    }
}