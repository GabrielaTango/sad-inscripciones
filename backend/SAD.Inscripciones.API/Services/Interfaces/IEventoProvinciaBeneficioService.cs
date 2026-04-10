using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IEventoProvinciaBeneficioService
{
    Task<IEnumerable<EventoProvinciaBeneficio>> GetAllAsync();
    Task<IEnumerable<EventoProvinciaBeneficio>> GetByEventoIdAsync(int eventoId);
    Task<EventoProvinciaBeneficio> GetByIdAsync(int id);
    Task<int> CreateAsync(EventoProvinciaBeneficio entity);
    Task UpdateAsync(EventoProvinciaBeneficio entity);
    Task DeleteAsync(int id, string deletedBy);
}
