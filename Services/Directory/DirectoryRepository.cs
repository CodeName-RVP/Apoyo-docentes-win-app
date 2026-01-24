using System;
using System.Data;
using System.IO;
using System.Linq;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AppParaUniversidad.Services.Directory;

/// <summary>
/// Repository SQLite para el directorio de docentes.
/// </summary>
public sealed class DirectoryRepository : IDirectoryRepository
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
            CREATE TABLE IF NOT EXISTS Contacts(
                NombreVisible TEXT NOT NULL,
                NombreNormalizado TEXT NOT NULL PRIMARY KEY,
                Correo TEXT NOT NULL,
                Telefono TEXT NULL,
                Activo INTEGER NOT NULL,
                LastUpdatedUtc TEXT NULL
            );
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));

        var columns = await connection.QueryAsync<string>(new CommandDefinition("SELECT name FROM pragma_table_info('Contacts');", cancellationToken: ct));
        if (!columns.Any(c => string.Equals(c, "LastUpdatedUtc", StringComparison.OrdinalIgnoreCase)))
        {
            await connection.ExecuteAsync(new CommandDefinition("ALTER TABLE Contacts ADD COLUMN LastUpdatedUtc TEXT NULL;", cancellationToken: ct));
        }
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken ct = default)
    {
        EnsureFolder();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);

        var sql = "SELECT NombreVisible, NombreNormalizado, Correo, Telefono, Activo, LastUpdatedUtc FROM Contacts ORDER BY NombreVisible";
        var rows = await connection.QueryAsync<Contact>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task UpsertAsync(Contact contact, CancellationToken ct = default)
    {
        if (contact is null) throw new ArgumentNullException(nameof(contact));

        var normalized = NameNormalizer.Normalize(string.IsNullOrWhiteSpace(contact.NombreNormalizado)
            ? contact.NombreVisible
            : contact.NombreNormalizado);

        var toSave = new Contact(
            nombreVisible: contact.NombreVisible,
            nombreNormalizado: normalized,
            correo: contact.Correo,
            telefono: contact.Telefono,
            activo: contact.Activo);

        EnsureFolder();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);

        var sql = """
            INSERT INTO Contacts (NombreVisible, NombreNormalizado, Correo, Telefono, Activo, LastUpdatedUtc)
            VALUES (@NombreVisible, @NombreNormalizado, @Correo, @Telefono, @Activo, @LastUpdatedUtc)
            ON CONFLICT(NombreNormalizado) DO UPDATE SET
                NombreVisible = excluded.NombreVisible,
                Correo = excluded.Correo,
                Telefono = excluded.Telefono,
                Activo = excluded.Activo,
                LastUpdatedUtc = excluded.LastUpdatedUtc;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, toSave, cancellationToken: ct));
    }

    public async Task DeleteAsync(string nombreNormalizado, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombreNormalizado)) return;

        var normalized = NameNormalizer.Normalize(nombreNormalizado);

        EnsureFolder();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);

        var sql = "DELETE FROM Contacts WHERE NombreNormalizado = @NombreNormalizado";
        await connection.ExecuteAsync(new CommandDefinition(sql, new { NombreNormalizado = normalized }, cancellationToken: ct));
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





