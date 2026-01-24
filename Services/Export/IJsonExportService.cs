using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Export;

public interface IJsonExportService
{
    Task ExportAsync(string path, IReadOnlyList<Contact> contacts, IReadOnlyList<SendLog> logs, CancellationToken ct = default);
    Task<ExportPackage> ImportAsync(string path, CancellationToken ct = default);
}

public sealed record ExportPackage(
    IReadOnlyList<Contact> Contacts,
    IReadOnlyList<SendLog> Logs);
