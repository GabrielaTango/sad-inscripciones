using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario?> GetByUsernameAsync(string username);
    Task<int> CreateAsync(Usuario entity);
    Task<bool> UpdateAsync(Usuario entity);
    Task<bool> UpdatePasswordAsync(int id, string passwordHash, string updatedBy);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
