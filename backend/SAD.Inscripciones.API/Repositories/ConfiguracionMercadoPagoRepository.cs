using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class ConfiguracionMercadoPagoRepository : IConfiguracionMercadoPagoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public ConfiguracionMercadoPagoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ConfiguracionMercadoPago> GetAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<ConfiguracionMercadoPago>(
            "SELECT * FROM ConfiguracionMercadoPago WHERE Id = 1");
        return row ?? new ConfiguracionMercadoPago();
    }

    public async Task UpdateAsync(ConfiguracionMercadoPago config)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE ConfiguracionMercadoPago SET
                AccessTokenCifrado = @AccessTokenCifrado,
                FrontendBaseUrl = @FrontendBaseUrl,
                UpdatedAt = UTC_TIMESTAMP(),
                UpdatedBy = @UpdatedBy
            WHERE Id = 1";
        await connection.ExecuteAsync(sql, config);
    }
}
