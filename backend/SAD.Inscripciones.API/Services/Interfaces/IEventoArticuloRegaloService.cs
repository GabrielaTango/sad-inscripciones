using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IEventoArticuloRegaloService
{
    Task<IEnumerable<EventoArticuloRegalo>> GetAllAsync();
    Task<IEnumerable<EventoArticuloRegalo>> GetByEventoIdAsync(int eventoId);
    Task<EventoArticuloRegalo> GetByIdAsync(int id);
    Task<int> CreateAsync(EventoArticuloRegalo entity);
    Task UpdateAsync(EventoArticuloRegalo entity);
    Task DeleteAsync(int id, string deletedBy);
}
