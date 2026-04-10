using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IEventoProvinciaBeneficioRepository
{
    Task<IEnumerable<EventoProvinciaBeneficio>> GetAllAsync();
    Task<EventoProvinciaBeneficio?> GetByIdAsync(int id);
    Task<IEnumerable<EventoProvinciaBeneficio>> GetByEventoIdAsync(int eventoId);
    Task<EventoProvinciaBeneficio?> GetByEventoAndProvinciaAsync(int eventoId, string provinciaCodigo);
    Task<int> CreateAsync(EventoProvinciaBeneficio entity);
    Task<bool> UpdateAsync(EventoProvinciaBeneficio entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
