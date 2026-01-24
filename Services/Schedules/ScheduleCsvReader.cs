using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;
using Microsoft.VisualBasic.FileIO;

namespace AppParaUniversidad.Services.Schedules;

public sealed class ScheduleCsvReader : IScheduleReader
{
    public ScheduleReadResult Read(string filePath, string sheetName)
    {
        var result = new ScheduleReadResult();
        try
        {
            if (!File.Exists(filePath))
            {
                result.Errors.Add("El archivo no existe.");
                return result;
            }

            var rows = ReadCsv(filePath);
            if (rows.Count == 0)
            {
                result.Errors.Add("El archivo CSV esta vacio.");
                return result;
            }

            var headerInfo = DetectHeader(rows, result);
            if (headerInfo is null)
            {
                return result;
            }

            for (var row = headerInfo.HeaderRow + 1; row < rows.Count; row++)
            {
                var rowValues = rows[row];
                if (headerInfo.NameColumn >= rowValues.Length)
                {
                    continue;
                }

                var nombreVisible = rowValues[headerInfo.NameColumn].Trim();
                if (string.IsNullOrWhiteSpace(nombreVisible))
                {
                    continue;
                }

                var schedule = new TeacherSchedule
                {
                    NombreVisible = nombreVisible,
                    NombreNormalizado = NameNormalizer.Normalize(nombreVisible)
                };

                foreach (var slot in headerInfo.Slots)
                {
                    if (slot.ColumnIndex >= rowValues.Length)
                    {
                        continue;
                    }

                    var cellValue = rowValues[slot.ColumnIndex].Trim();
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        continue;
                    }

                    var days = ScheduleParser.ParseDays(cellValue);
                    foreach (var day in days)
                    {
                        if (!schedule.SlotsPorDia.TryGetValue(day, out var list))
                        {
                            list = new List<TimeRange>();
                            schedule.SlotsPorDia[day] = list;
                        }

                        list.Add(new TimeRange(slot.Start, slot.End));
                    }
                }

                foreach (var pref in headerInfo.PreferenceColumns)
                {
                    if (pref.ColumnIndex >= rowValues.Length)
                    {
                        continue;
                    }

                    var value = rowValues[pref.ColumnIndex].Trim();
                    if (ScheduleParser.TryParsePriority(value, out var priority))
                    {
                        schedule.SubjectPreferences.Add(new SubjectPreference
                        {
                            Subject = pref.HeaderText,
                            Priority = priority
                        });
                    }
                }

                ScheduleParser.CompactAndCalculate(schedule);
                result.Schedules.Add(schedule);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(ScheduleCsvReader), ex);
            result.Errors.Add("Error leyendo el archivo CSV.");
        }

        return result;
    }

    public List<string> ListSheetNames(string filePath)
    {
        return ["CSV"];
    }

    private static ScheduleHeaderInfo? DetectHeader(List<string[]> rows, ScheduleReadResult result)
    {
        var maxRow = Math.Min(50, rows.Count);
        for (var row = 0; row < maxRow; row++)
        {
            var values = rows[row];
            var nameColumn = -1;
            var slots = new List<ScheduleSlotHeader>();
            var candidates = new List<SubjectPreferenceColumn>();

            for (var col = 0; col < values.Length; col++)
            {
                var text = values[col].Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var normalized = NameNormalizer.Normalize(text);
                if (nameColumn < 0 && (normalized.Contains("nombre") || normalized.Contains("docente")))
                {
                    nameColumn = col;
                    continue;
                }

                if (ScheduleParser.TryParseSlotHeader(text, out var start, out var end))
                {
                    slots.Add(new ScheduleSlotHeader(col, start, end, text));
                    continue;
                }

                candidates.Add(new SubjectPreferenceColumn(col, text));
            }

            if (nameColumn >= 0 && slots.Count > 0)
            {
                var preferenceColumns = DetectPreferenceColumns(rows, row, candidates);

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
        List<string[]> rows,
        int headerRow,
        List<SubjectPreferenceColumn> candidates)
    {
        var result = new List<SubjectPreferenceColumn>();
        var maxRow = Math.Min(rows.Count - 1, headerRow + 50);

        foreach (var candidate in candidates)
        {
            for (var row = headerRow + 1; row <= maxRow; row++)
            {
                if (candidate.ColumnIndex >= rows[row].Length)
                {
                    continue;
                }

                var value = rows[row][candidate.ColumnIndex].Trim();
                if (ScheduleParser.TryParsePriority(value, out _))
                {
                    result.Add(candidate);
                    break;
                }
            }
        }

        return result;
    }

    private static List<string[]> ReadCsv(string filePath)
    {
        var rows = new List<string[]>();
        var delimiter = DetectDelimiter(filePath);

        using var parser = new TextFieldParser(filePath)
        {
            TextFieldType = FieldType.Delimited,
            Delimiters = [delimiter.ToString()],
            HasFieldsEnclosedInQuotes = true
        };

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields() ?? [];
            rows.Add(fields);
        }

        return rows;
    }

    private static char DetectDelimiter(string filePath)
    {
        var firstLine = File.ReadLines(filePath).FirstOrDefault() ?? string.Empty;
        var commaCount = firstLine.Count(c => c == ',');
        var semiCount = firstLine.Count(c => c == ';');
        return semiCount > commaCount ? ';' : ',';
    }
}
