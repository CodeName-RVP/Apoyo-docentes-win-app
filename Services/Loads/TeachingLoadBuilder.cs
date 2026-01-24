using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Loads;

public sealed class TeachingLoadBuilder : ITeachingLoadBuilder
{
    public IReadOnlyList<DocenteCarga> BuildByDocente(IEnumerable<TeachingAssignment> assignments)
    {
        var grouped = assignments
            .Where(a => !string.IsNullOrWhiteSpace(a.DocenteOriginal))
            .GroupBy(a => NameNormalizer.Normalize(a.DocenteOriginal));

        var result = new List<DocenteCarga>();
        foreach (var group in grouped)
        {
            var items = group.ToList();
            var total = items.Sum(i => i.Horas);
            var visibleName = items.First().DocenteOriginal.Trim();

            result.Add(new DocenteCarga(
                NombreVisible: visibleName,
                NombreNormalizado: group.Key,
                Items: items,
                TotalHoras: total));
        }
        return result;
    }
}
