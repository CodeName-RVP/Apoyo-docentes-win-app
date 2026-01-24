using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;
using ClosedXML.Excel;

namespace AppParaUniversidad.Services.Excel;

public sealed class ExcelReader : IExcelReader
{
    private static readonly string[] LogicalColumns =
    {
        "asignatura", "grupo", "horas", "salon",
        "lu", "ma", "mi", "ju", "vi",
        "profesor", "docente", "profesora",
        "tipo", "pa", "ptc", "comision"
    };

    public Task<IReadOnlyList<string>> ListSheetNamesAsync(string path, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(path);
        return Task.FromResult<IReadOnlyList<string>>(wb.Worksheets.Select(ws => ws.Name).ToList());
    }

    public Task<ExcelDetectionResult> DetectAndReadAsync(string path, string sheetName, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase))
                 ?? throw new InvalidOperationException($"Hoja '{sheetName}' no encontrada.");

        var headerRowIdx = DetectHeaderRow(ws);
        if (headerRowIdx == -1)
        {
            throw new InvalidOperationException("No se detectaron encabezados en las primeras 50 filas.");
        }

        var columnMap = MapColumns(ws.Row(headerRowIdx));
        var warnings = new List<string>();
        var rows = ReadRows(ws, headerRowIdx, columnMap, warnings);

        return Task.FromResult(new ExcelDetectionResult(headerRowIdx, columnMap, rows, warnings));
    }

    private int DetectHeaderRow(IXLWorksheet ws)
    {
        for (var r = 1; r <= 50; r++)
        {
            var cells = ws.Row(r).CellsUsed().ToList();
            if (cells.Count < 3) continue;
            var score = cells.Sum(c => ScoreHeaderName(c.GetString()));
            if (score >= 3) return r;
        }
        return -1;
    }

    private int ScoreHeaderName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var norm = NameNormalizer.Normalize(value);
        return LogicalColumns.Any(lc => norm.Contains(lc)) ? 1 : 0;
    }

    private IReadOnlyDictionary<string, int> MapColumns(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>();
        foreach (var cell in headerRow.CellsUsed())
        {
            var norm = NameNormalizer.Normalize(cell.GetString());
            if (string.IsNullOrEmpty(norm)) continue;

            if (norm.Contains("asignatura")) map.TryAdd("asignatura", cell.Address.ColumnNumber);
            else if (norm.Contains("grupo")) map.TryAdd("grupo", cell.Address.ColumnNumber);
            else if (norm.Contains("hora")) map.TryAdd("horas", cell.Address.ColumnNumber);
            else if (norm.Contains("salon") || norm.Contains("aula")) map.TryAdd("salon", cell.Address.ColumnNumber);
            else if (norm == "lu") map.TryAdd("lu", cell.Address.ColumnNumber);
            else if (norm == "ma") map.TryAdd("ma", cell.Address.ColumnNumber);
            else if (norm == "mi") map.TryAdd("mi", cell.Address.ColumnNumber);
            else if (norm == "ju") map.TryAdd("ju", cell.Address.ColumnNumber);
            else if (norm == "vi") map.TryAdd("vi", cell.Address.ColumnNumber);
            else if (norm.Contains("profesor") || norm.Contains("docente")) map.TryAdd("docente", cell.Address.ColumnNumber);
            else if (norm.Contains("comision")) map.TryAdd("comision", cell.Address.ColumnNumber);
            else if (norm.Contains("ptc") || norm.Contains("pa") || norm.Contains("tipo")) map.TryAdd("tipo", cell.Address.ColumnNumber);
        }
        return map;
    }

    private IReadOnlyList<TeachingAssignment> ReadRows(IXLWorksheet ws, int headerRowIdx, IReadOnlyDictionary<string, int> map, List<string> warnings)
    {
        var list = new List<TeachingAssignment>();
        foreach (var row in ws.RowsUsed().Where(r => r.RowNumber() > headerRowIdx))
        {
            var docenteRaw = Get(row, map, "docente");
            if (string.IsNullOrWhiteSpace(docenteRaw)) continue;

            var docenteNorm = NameNormalizer.Normalize(docenteRaw);
            var horas = decimal.TryParse(Get(row, map, "horas"), out var h) ? h : 0m;

            list.Add(new TeachingAssignment(
                Asignatura: Get(row, map, "asignatura"),
                Grupo: Get(row, map, "grupo"),
                Horas: horas,
                Salon: Get(row, map, "salon"),
                Lu: Get(row, map, "lu"),
                Ma: Get(row, map, "ma"),
                Mi: Get(row, map, "mi"),
                Ju: Get(row, map, "ju"),
                Vi: Get(row, map, "vi"),
                Tipo: Get(row, map, "tipo"),
                Comision: Get(row, map, "comision"),
                DocenteOriginal: docenteRaw,
                DocenteNormalizado: docenteNorm));
        }
        return list;
    }

    private string Get(IXLRow row, IReadOnlyDictionary<string, int> map, string key)
    {
        return map.TryGetValue(key, out var col) ? row.Cell(col).GetString().Trim() : string.Empty;
    }
}
