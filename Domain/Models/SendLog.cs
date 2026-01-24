using System;

namespace AppParaUniversidad.Domain.Models;

public sealed record SendLog(
    DateTime Fecha,
    string NombreNormalizado,
    string NombreVisible,
    string Correo,
    string Estado,
    string? Error);
