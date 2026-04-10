using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IEventoPrecioRepository
{
    Task<IEnumerable<EventoPrecio>> GetAllAsync();
    Task<EventoPrecio?> GetByIdAsync(int id);
    Task<IEnumerable<EventoPrecio>> GetByEventoIdAsync(int eventoId);
    Task<EventoPrecio?> GetByEventoAndTipoAlumnoAsync(int eventoId, int tipoAlumnoId);
    Task<int> CreateAsync(EventoPrecio entity);
    Task<bool> UpdateAsync(EventoPrecio entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
