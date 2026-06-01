using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IConfiguracionPayPalRepository
{
    Task<ConfiguracionPayPal> GetAsync();
    Task UpdateAsync(ConfiguracionPayPal config);
}
