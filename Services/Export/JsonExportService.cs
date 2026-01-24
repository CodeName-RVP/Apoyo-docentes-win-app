using System.IO;
using System.Text.Json;
using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Export;

public sealed class JsonExportService : IJsonExportService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public async Task ExportAsync(string path, IReadOnlyList<Contact> contacts, IReadOnlyList<SendLog> logs, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Ruta inválida", nameof(path));
        var package = new ExportPackage(contacts, logs);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, package, Options, ct);
    }

    public async Task<ExportPackage> ImportAsync(string path, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(path);
        var package = await JsonSerializer.DeserializeAsync<ExportPackage>(stream, Options, ct);
        if (package is null) throw new InvalidOperationException("Archivo de importación inválido.");
        return package;
    }
}
