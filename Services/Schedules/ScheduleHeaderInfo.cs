using System.Collections.Generic;
using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Schedules;

public sealed class ScheduleHeaderInfo
{
    public int HeaderRow { get; set; }
    public int NameColumn { get; set; }
    public List<ScheduleSlotHeader> Slots { get; set; } = new();
    public List<SubjectPreferenceColumn> PreferenceColumns { get; set; } = new();
}
