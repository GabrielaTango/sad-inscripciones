using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface ITipoEventoRepository
{
    Task<IEnumerable<TipoEvento>> GetAllAsync();
    Task<TipoEvento?> GetByIdAsync(int id);
    Task<TipoEvento?> GetByNombreAsync(string nombre);
    Task<int> CreateAsync(TipoEvento entity);
    Task<bool> UpdateAsync(TipoEvento entity);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
