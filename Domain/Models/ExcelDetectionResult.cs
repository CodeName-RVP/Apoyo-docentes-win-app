using System.Collections.Generic;

namespace AppParaUniversidad.Domain.Models;

public sealed record ExcelDetectionResult(
    int HeaderRowIndex,
    IReadOnlyDictionary<string, int> ColumnMap, // nombre logico -> indice
    IReadOnlyList<TeachingAssignment> Rows,
    IReadOnlyList<string> Warnings);
