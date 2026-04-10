using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IPromocionRepository
{
    Task<IEnumerable<Promocion>> GetAllAsync();
    Task<Promocion?> GetByIdAsync(int id);
    Task<IEnumerable<Promocion>> GetActiveAsync();
    Task<int> CreateAsync(Promocion entity);
    Task<bool> UpdateAsync(Promocion entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
