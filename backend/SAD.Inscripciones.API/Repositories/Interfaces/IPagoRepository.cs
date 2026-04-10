using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IPagoRepository
{
    Task<IEnumerable<Pago>> GetAllAsync();
    Task<Pago?> GetByIdAsync(int id);
    Task<IEnumerable<Pago>> GetByInscripcionIdAsync(int inscripcionId);
    Task<int> CreateAsync(Pago entity);
    Task<bool> UpdateAsync(Pago entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
