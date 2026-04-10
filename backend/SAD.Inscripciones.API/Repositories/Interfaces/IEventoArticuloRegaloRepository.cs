using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IEventoArticuloRegaloRepository
{
    Task<IEnumerable<EventoArticuloRegalo>> GetAllAsync();
    Task<EventoArticuloRegalo?> GetByIdAsync(int id);
    Task<IEnumerable<EventoArticuloRegalo>> GetByEventoIdAsync(int eventoId);
    Task<int> CreateAsync(EventoArticuloRegalo entity);
    Task<bool> UpdateAsync(EventoArticuloRegalo entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
