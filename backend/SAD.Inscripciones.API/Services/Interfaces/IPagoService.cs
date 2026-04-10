using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IPagoService
{
    Task<IEnumerable<Pago>> GetAllAsync();
    Task<Pago> GetByIdAsync(int id);
    Task<IEnumerable<Pago>> GetByInscripcionIdAsync(int inscripcionId);
    Task<int> CreateAsync(Pago entity);
    Task UpdateEstadoAsync(int id, string estadoPago, string updatedBy);
    Task DeleteAsync(int id, string deletedBy);
}
