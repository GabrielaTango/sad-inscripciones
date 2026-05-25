using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class ConfiguracionContactoRepository : IConfiguracionContactoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public ConfiguracionContactoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ConfiguracionContacto> GetAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<ConfiguracionContacto>(
            "SELECT * FROM ConfiguracionContacto WHERE Id = 1");
        return row ?? new ConfiguracionContacto();
    }

    public async Task UpdateAsync(ConfiguracionContacto config)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE ConfiguracionContacto SET
                EmailDestino = @EmailDestino,
                Activo = @Activo,
                UpdatedAt = UTC_TIMESTAMP(),
                UpdatedBy = @UpdatedBy
            WHERE Id = 1";
        await connection.ExecuteAsync(sql, config);
    }
}
