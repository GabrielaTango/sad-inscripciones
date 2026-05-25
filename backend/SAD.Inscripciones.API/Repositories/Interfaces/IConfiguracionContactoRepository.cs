using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IConfiguracionContactoRepository
{
    Task<ConfiguracionContacto> GetAsync();
    Task UpdateAsync(ConfiguracionContacto config);
}
