using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface ITipoAlumnoService
{
    Task<IEnumerable<TipoAlumno>> GetAllAsync();
    Task<TipoAlumno> GetByIdAsync(int id);
    Task<int> CreateAsync(TipoAlumno entity);
    Task UpdateAsync(TipoAlumno entity);
    Task DeleteAsync(int id, string deletedBy);
}
