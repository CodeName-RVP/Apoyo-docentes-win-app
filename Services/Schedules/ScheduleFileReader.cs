using System;
using System.Collections.Generic;
using System.IO;
using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Schedules;

public sealed class ScheduleFileReader : IScheduleReader
{
    private readonly IScheduleReader _excelReader = new ScheduleExcelReader();
    private readonly IScheduleReader _csvReader = new ScheduleCsvReader();

    public ScheduleReadResult Read(string filePath, string sheetName)
    {
        return SelectReader(filePath).Read(filePath, sheetName);
    }

    public List<string> ListSheetNames(string filePath)
    {
        return SelectReader(filePath).ListSheetNames(filePath);
    }

    private IScheduleReader SelectReader(string filePath)
    {
        var ext = Path.GetExtension(filePath) ?? string.Empty;
        if (ext.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return _csvReader;
        }

        return _excelReader;
    }
}
