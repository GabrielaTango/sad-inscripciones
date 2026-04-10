using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repository;

    public UsuarioService(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<Usuario> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("Usuario", id);
    }

    public async Task<int> CreateAsync(Usuario entity, string password)
    {
        var existing = await _repository.GetByUsernameAsync(entity.Username);
        if (existing != null)
            throw new BusinessException($"Ya existe un usuario con el nombre '{entity.Username}'.");

        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(Usuario entity)
    {
        await GetByIdAsync(entity.Id);
        var existing = await _repository.GetByUsernameAsync(entity.Username);
        if (existing != null && existing.Id != entity.Id)
            throw new BusinessException($"Ya existe un usuario con el nombre '{entity.Username}'.");
        await _repository.UpdateAsync(entity);
    }

    public async Task CambiarPasswordAsync(int id, string passwordActual, string passwordNueva, string updatedBy)
    {
        var usuario = await GetByIdAsync(id);
        if (!BCrypt.Net.BCrypt.Verify(passwordActual, usuario.PasswordHash))
            throw new BusinessException("La contraseña actual es incorrecta.");

        var newHash = BCrypt.Net.BCrypt.HashPassword(passwordNueva);
        await _repository.UpdatePasswordAsync(id, newHash, updatedBy);
    }

    public async Task ResetPasswordAsync(int id, string passwordNueva, string updatedBy)
    {
        await GetByIdAsync(id);
        var newHash = BCrypt.Net.BCrypt.HashPassword(passwordNueva);
        await _repository.UpdatePasswordAsync(id, newHash, updatedBy);
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }

    public async Task<Usuario?> ValidateCredentialsAsync(string username, string password)
    {
        var usuario = await _repository.GetByUsernameAsync(username);
        if (usuario == null || !usuario.Activo)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
            return null;

        return usuario;
    }

    public async Task SeedAdminAsync()
    {
        var existing = await _repository.GetByUsernameAsync("admin");
        if (existing != null)
            return;

        var admin = new Usuario
        {
            Username = "admin",
            NombreCompleto = "Administrador",
            Email = null,
            Activo = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            CreatedBy = "system",
            UpdatedBy = "system"
        };

        await _repository.CreateAsync(admin);
    }
}
