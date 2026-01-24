using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Loads;

public interface ITeachingLoadBuilder
{
    IReadOnlyList<DocenteCarga> BuildByDocente(IEnumerable<TeachingAssignment> assignments);
}
