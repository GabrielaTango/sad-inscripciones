using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public EmailTemplateRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<EmailTemplate>> GetAllAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.QueryAsync<EmailTemplate>(
            "SELECT Id, Codigo, Nombre, Asunto, BodyHtml, BodyJson, Activo, CreatedAt, UpdatedAt, UpdatedBy FROM EmailTemplates ORDER BY Nombre");
    }

    public async Task<EmailTemplate?> GetByCodigoAsync(string codigo)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EmailTemplate>(
            "SELECT Id, Codigo, Nombre, Asunto, BodyHtml, BodyJson, Activo, CreatedAt, UpdatedAt, UpdatedBy FROM EmailTemplates WHERE Codigo = @Codigo",
            new { Codigo = codigo });
    }

    public async Task UpdateAsync(EmailTemplate template)
    {
        using var conn = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE EmailTemplates SET
                Asunto = @Asunto,
                BodyHtml = @BodyHtml,
                BodyJson = @BodyJson,
                Activo = @Activo,
                UpdatedAt = UTC_TIMESTAMP(),
                UpdatedBy = @UpdatedBy
            WHERE Codigo = @Codigo";
        await conn.ExecuteAsync(sql, template);
    }
}
