using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class ConfiguracionEmailRepository : IConfiguracionEmailRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public ConfiguracionEmailRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ConfiguracionEmail> GetAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<ConfiguracionEmail>(
            "SELECT * FROM ConfiguracionEmail WHERE Id = 1");
        return row ?? new ConfiguracionEmail();
    }

    public async Task UpdateAsync(ConfiguracionEmail config)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE ConfiguracionEmail SET
                Host = @Host,
                Port = @Port,
                EnableSsl = @EnableSsl,
                Usuario = @Usuario,
                PasswordCifrado = @PasswordCifrado,
                FromEmail = @FromEmail,
                FromName = @FromName,
                ReplyTo = @ReplyTo,
                BccCopia = @BccCopia,
                Asunto = @Asunto,
                Activo = @Activo,
                IgnorarCertificadoSsl = @IgnorarCertificadoSsl,
                UpdatedAt = UTC_TIMESTAMP(),
                UpdatedBy = @UpdatedBy
            WHERE Id = 1";
        await connection.ExecuteAsync(sql, config);
    }
}
