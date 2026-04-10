using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IContactoRepository
{
    Task<IEnumerable<Contacto>> GetAllAsync();
    Task<Contacto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Contacto contacto);
    Task<bool> UpdateAsync(Contacto contacto);
    Task<bool> DeleteAsync(int id);
}
