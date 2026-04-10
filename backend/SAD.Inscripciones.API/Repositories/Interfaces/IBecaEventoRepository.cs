using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IBecaEventoRepository
{
    Task<IEnumerable<BecaEvento>> GetAllAsync();
    Task<BecaEvento?> GetByIdAsync(int id);
    Task<IEnumerable<BecaEvento>> GetByEventoIdAsync(int eventoId);
    Task<int> CreateAsync(BecaEvento entity);
    Task<bool> UpdateAsync(BecaEvento entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
