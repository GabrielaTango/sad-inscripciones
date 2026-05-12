using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IConfiguracionMercadoPagoRepository
{
    Task<ConfiguracionMercadoPago> GetAsync();
    Task UpdateAsync(ConfiguracionMercadoPago config);
}
