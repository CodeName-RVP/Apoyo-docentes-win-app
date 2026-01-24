using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Directory;

public interface IDirectoryRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken ct = default);
    Task UpsertAsync(Contact contact, CancellationToken ct = default);
    Task DeleteAsync(string nombreNormalizado, CancellationToken ct = default);
}
