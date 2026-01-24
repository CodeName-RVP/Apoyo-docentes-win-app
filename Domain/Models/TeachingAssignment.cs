namespace AppParaUniversidad.Domain.Models;

public sealed record TeachingAssignment(
    string Asignatura,
    string Grupo,
    decimal Horas,
    string Salon,
    string Lu,
    string Ma,
    string Mi,
    string Ju,
    string Vi,
    string Tipo,
    string Comision,
    string DocenteOriginal,
    string DocenteNormalizado);
