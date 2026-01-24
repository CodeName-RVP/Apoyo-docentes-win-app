using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Excel;

public interface IExcelReader
{
    Task<IReadOnlyList<string>> ListSheetNamesAsync(string path, CancellationToken ct = default);
    Task<ExcelDetectionResult> DetectAndReadAsync(string path, string sheetName, CancellationToken ct = default);
}
