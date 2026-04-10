using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario> GetByIdAsync(int id);
    Task<int> CreateAsync(Usuario entity, string password);
    Task UpdateAsync(Usuario entity);
    Task CambiarPasswordAsync(int id, string passwordActual, string passwordNueva, string updatedBy);
    Task ResetPasswordAsync(int id, string passwordNueva, string updatedBy);
    Task DeleteAsync(int id, string deletedBy);
    Task<Usuario?> ValidateCredentialsAsync(string username, string password);
    Task SeedAdminAsync();
}
