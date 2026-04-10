using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IEventoService
{
    Task<IEnumerable<Evento>> GetAllAsync();
    Task<IEnumerable<Evento>> GetActivosAsync();
    Task<Evento> GetByIdAsync(int id);
    Task<int> CreateAsync(Evento entity);
    Task UpdateAsync(Evento entity);
    Task DeleteAsync(int id, string deletedBy);
}
