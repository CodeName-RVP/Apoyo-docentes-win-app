using System.Collections.Generic;

namespace AppParaUniversidad.Domain.Models;

public sealed class TeacherSchedule
{
    public string NombreVisible { get; set; } = string.Empty;
    public string NombreNormalizado { get; set; } = string.Empty;
    public Dictionary<string, List<TimeRange>> SlotsPorDia { get; } = new();
    public Dictionary<string, List<TimeRange>> BloquesPorDia { get; } = new();
    public Dictionary<string, double> HorasPorDia { get; } = new();
    public double TotalSemanalHoras { get; set; }
    public List<SubjectPreference> SubjectPreferences { get; } = new();
}
