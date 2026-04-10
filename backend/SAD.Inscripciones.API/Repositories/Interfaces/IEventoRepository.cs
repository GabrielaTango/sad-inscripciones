using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IEventoRepository
{
    Task<IEnumerable<Evento>> GetAllAsync();
    Task<Evento?> GetByIdAsync(int id);
    Task<IEnumerable<Evento>> GetActivosAsync();
    Task<int> CreateAsync(Evento entity);
    Task<bool> UpdateAsync(Evento entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
    Task<bool> UpdateTerminosArchivoAsync(int id, string? archivo);
}
