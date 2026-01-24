using System.Data;
using System.IO;
using AppParaUniversidad.Domain.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AppParaUniversidad.Services.Sheets;

public sealed class SheetHistoryRepository : ISheetHistoryRepository
{
    private const string DatabaseFileName = "app.db";
    private const string DatabaseFolderName = "AppParaUniversidad";

    private static string DbPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DatabaseFolderName, DatabaseFileName);

    private static string ConnectionString => $"Data Source={DbPath}";

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        EnsureFolder();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);

        var sql = """
            CREATE TABLE IF NOT EXISTS SheetIds(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                SheetId TEXT NOT NULL UNIQUE,
                CreatedAtUtc TEXT NOT NULL
            );
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<SheetIdEntry>> GetAllAsync(CancellationToken ct = default)
    {
        EnsureFolder();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);

        var sql = "SELECT Id, Name, SheetId, CreatedAtUtc FROM SheetIds ORDER BY Name";
        var rows = await connection.QueryAsync<SheetIdEntry>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<SheetIdEntry?> GetBySheetIdAsync(string sheetId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sheetId)) return null;

        EnsureFolder();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);

        var sql = "SELECT Id, Name, SheetId, CreatedAtUtc FROM SheetIds WHERE SheetId = @SheetId";
        return await connection.QueryFirstOrDefaultAsync<SheetIdEntry>(new CommandDefinition(sql, new { SheetId = sheetId.Trim() }, cancellationToken: ct));
    }

    public async Task UpsertAsync(SheetIdEntry entry, CancellationToken ct = default)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));

        EnsureFolder();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);

        var sql = """
            INSERT INTO SheetIds (Name, SheetId, CreatedAtUtc)
            VALUES (@Name, @SheetId, @CreatedAtUtc)
            ON CONFLICT(SheetId) DO UPDATE SET
                Name = excluded.Name;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, entry, cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        EnsureFolder();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);

        var sql = "DELETE FROM SheetIds WHERE Id = @Id";
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    private static void EnsureFolder()
    {
        var folder = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(folder) && !System.IO.Directory.Exists(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
        }
    }
}
