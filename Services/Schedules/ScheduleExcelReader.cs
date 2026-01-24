using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Schedules;

public sealed class ScheduleExcelReader : IScheduleReader
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

            using var workbook = new XLWorkbook(filePath);
            if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var sheet))
            {
                result.Errors.Add("No se encontro la hoja seleccionada.");
                return result;
            }

            var headerInfo = ScheduleHeaderDetector.Detect(sheet, result);
            if (headerInfo is null)
            {
                return result;
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerInfo.HeaderRow;
            for (var row = headerInfo.HeaderRow + 1; row <= lastRow; row++)
            {
                var nameCell = sheet.Row(row).Cell(headerInfo.NameColumn);
                var nombreVisible = nameCell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(nombreVisible))
                {
                    continue;
                }

                var schedule = new TeacherSchedule
                {
                    NombreVisible = nombreVisible,
                    NombreNormalizado = NameNormalizer.Normalize(nombreVisible),
                };

                foreach (var slot in headerInfo.Slots)
                {
                    var cellValue = sheet.Row(row).Cell(slot.ColumnIndex).GetString().Trim();
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
                    var value = sheet.Row(row).Cell(pref.ColumnIndex).GetString().Trim();
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
            Logger.LogError(nameof(ScheduleExcelReader), ex);
            result.Errors.Add("Error leyendo el archivo de horarios.");
        }

        return result;
    }

    public List<string> ListSheetNames(string filePath)
    {
        var names = new List<string>();
        try
        {
            using var workbook = new XLWorkbook(filePath);
            foreach (var sheet in workbook.Worksheets)
            {
                names.Add(sheet.Name);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(ListSheetNames), ex);
        }

        return names;
    }
}
