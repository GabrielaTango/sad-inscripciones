using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IBecaEventoService
{
    Task<IEnumerable<BecaEvento>> GetAllAsync();
    Task<BecaEvento> GetByIdAsync(int id);
    Task<IEnumerable<BecaEvento>> GetByEventoIdAsync(int eventoId);
    Task<int> CreateAsync(BecaEvento entity, string createdBy);
    Task UpdateAsync(BecaEvento entity);
    Task DeleteAsync(int id, string deletedBy);
}
