using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class ConfiguracionPayPalRepository : IConfiguracionPayPalRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public ConfiguracionPayPalRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ConfiguracionPayPal> GetAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<ConfiguracionPayPal>(
            "SELECT * FROM ConfiguracionPayPal WHERE Id = 1");
        return row ?? new ConfiguracionPayPal();
    }

    public async Task UpdateAsync(ConfiguracionPayPal config)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE ConfiguracionPayPal SET
                ClientId = @ClientId,
                Moneda = @Moneda,
                UpdatedAt = UTC_TIMESTAMP(),
                UpdatedBy = @UpdatedBy
            WHERE Id = 1";
        await connection.ExecuteAsync(sql, config);
    }
}
