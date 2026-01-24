using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Sheets;

public interface ISheetHistoryRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SheetIdEntry>> GetAllAsync(CancellationToken ct = default);
    Task<SheetIdEntry?> GetBySheetIdAsync(string sheetId, CancellationToken ct = default);
    Task UpsertAsync(SheetIdEntry entry, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
