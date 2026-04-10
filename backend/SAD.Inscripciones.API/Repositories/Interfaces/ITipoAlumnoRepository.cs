using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface ITipoAlumnoRepository
{
    Task<IEnumerable<TipoAlumno>> GetAllAsync();
    Task<TipoAlumno?> GetByIdAsync(int id);
    Task<TipoAlumno?> GetByNombreAsync(string nombre);
    Task<int> CreateAsync(TipoAlumno entity);
    Task<bool> UpdateAsync(TipoAlumno entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
