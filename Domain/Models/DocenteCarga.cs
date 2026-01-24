using System.Collections.Generic;

namespace AppParaUniversidad.Domain.Models;

public sealed record DocenteCarga(
    string NombreVisible,
    string NombreNormalizado,
    IReadOnlyList<TeachingAssignment> Items,
    decimal TotalHoras);
