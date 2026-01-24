using AppParaUniversidad.Domain.Models;

using System.Collections.Generic;

namespace AppParaUniversidad.Services.Schedules;

public interface IScheduleReader
{
    ScheduleReadResult Read(string filePath, string sheetName);
    List<string> ListSheetNames(string filePath);
}

