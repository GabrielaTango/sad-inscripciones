using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IConfiguracionEmailRepository
{
    Task<ConfiguracionEmail> GetAsync();
    Task UpdateAsync(ConfiguracionEmail config);
}
