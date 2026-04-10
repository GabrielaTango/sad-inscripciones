using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface ITipoEventoService
{
    Task<IEnumerable<TipoEvento>> GetAllAsync();
    Task<TipoEvento> GetByIdAsync(int id);
    Task<int> CreateAsync(TipoEvento entity);
    Task UpdateAsync(TipoEvento entity);
    Task DeleteAsync(int id, string deletedBy);
}
