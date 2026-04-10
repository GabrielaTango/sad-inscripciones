using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IEventoPrecioService
{
    Task<IEnumerable<EventoPrecio>> GetAllAsync();
    Task<IEnumerable<EventoPrecio>> GetByEventoIdAsync(int eventoId);
    Task<EventoPrecio> GetByIdAsync(int id);
    Task<int> CreateAsync(EventoPrecio entity);
    Task UpdateAsync(EventoPrecio entity);
    Task DeleteAsync(int id, string deletedBy);
}
