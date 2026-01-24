using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Schedules;

public static class ScheduleHeaderDetector
{
    public static ScheduleHeaderInfo? Detect(IXLWorksheet sheet, ScheduleReadResult result)
    {
        for (var row = 1; row <= 50; row++)
        {
            var headerRow = sheet.Row(row);
            var nameColumn = 0;
            var slots = new List<ScheduleSlotHeader>();
            var candidateColumns = new List<SubjectPreferenceColumn>();

            foreach (var cell in headerRow.CellsUsed())
            {
                var text = cell.GetString().Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var normalized = NameNormalizer.Normalize(text);
                if (nameColumn == 0 && (normalized.Contains("nombre") || normalized.Contains("docente")))
                {
                    nameColumn = cell.Address.ColumnNumber;
                    continue;
                }

                if (ScheduleParser.TryParseSlotHeader(text, out var start, out var end))
                {
                    slots.Add(new ScheduleSlotHeader(cell.Address.ColumnNumber, start, end, text));
                    continue;
                }

                candidateColumns.Add(new SubjectPreferenceColumn(cell.Address.ColumnNumber, text));
            }

            if (nameColumn > 0 && slots.Count > 0)
            {
                var preferenceColumns = DetectPreferenceColumns(sheet, row, candidateColumns);

                return new ScheduleHeaderInfo
                {
                    HeaderRow = row,
                    NameColumn = nameColumn,
                    Slots = slots.OrderBy(s => s.Start).ThenBy(s => s.ColumnIndex).ToList(),
                    PreferenceColumns = preferenceColumns
                };
            }
        }

        result.Errors.Add("No se encontro fila de encabezados con columna de nombre y horarios.");
        return null;
    }

    private static List<SubjectPreferenceColumn> DetectPreferenceColumns(
        IXLWorksheet sheet,
        int headerRow,
        List<SubjectPreferenceColumn> candidates)
    {
        var result = new List<SubjectPreferenceColumn>();
        if (candidates.Count == 0)
        {
            return result;
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var maxRow = Math.Min(lastRow, headerRow + 50);

        foreach (var candidate in candidates)
        {
            for (var row = headerRow + 1; row <= maxRow; row++)
            {
                var value = sheet.Row(row).Cell(candidate.ColumnIndex).GetString().Trim();
                if (ScheduleParser.TryParsePriority(value, out _))
                {
                    result.Add(candidate);
                    break;
                }
            }
        }

        return result;
    }
}
