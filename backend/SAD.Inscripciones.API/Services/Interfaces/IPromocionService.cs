using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IPromocionService
{
    Task<IEnumerable<Promocion>> GetAllAsync();
    Task<Promocion> GetByIdAsync(int id);
    Task<int> CreateAsync(Promocion entity, string createdBy);
    Task UpdateAsync(Promocion entity);
    Task DeleteAsync(int id, string deletedBy);
}
