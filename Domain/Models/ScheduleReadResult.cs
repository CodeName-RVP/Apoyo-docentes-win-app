using System.Collections.Generic;

namespace AppParaUniversidad.Domain.Models;

public sealed class ScheduleReadResult
{
    public List<TeacherSchedule> Schedules { get; } = new();
    public List<string> Errors { get; } = new();
    public bool HasErrors => Errors.Count > 0;
}
