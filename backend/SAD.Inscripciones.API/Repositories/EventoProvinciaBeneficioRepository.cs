using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class EventoProvinciaBeneficioRepository : IEventoProvinciaBeneficioRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public EventoProvinciaBeneficioRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<EventoProvinciaBeneficio>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<EventoProvinciaBeneficio>(
            "SELECT * FROM EventoProvinciaBeneficios WHERE DeletedAt IS NULL");
    }

    public async Task<EventoProvinciaBeneficio?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EventoProvinciaBeneficio>(
            "SELECT * FROM EventoProvinciaBeneficios WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<EventoProvinciaBeneficio>> GetByEventoIdAsync(int eventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<EventoProvinciaBeneficio>(
            "SELECT * FROM EventoProvinciaBeneficios WHERE EventoId = @EventoId AND DeletedAt IS NULL",
            new { EventoId = eventoId });
    }

    public async Task<EventoProvinciaBeneficio?> GetByEventoAndProvinciaAsync(int eventoId, string provinciaCodigo)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EventoProvinciaBeneficio>(
            @"SELECT * FROM EventoProvinciaBeneficios
              WHERE EventoId = @EventoId AND ProvinciaCodigo = @ProvinciaCodigo AND DeletedAt IS NULL AND Activo = 1",
            new { EventoId = eventoId, ProvinciaCodigo = provinciaCodigo });
    }

    public async Task<int> CreateAsync(EventoProvinciaBeneficio entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO EventoProvinciaBeneficios (EventoId, ProvinciaCodigo, AplicaPrecioSocio, PorcentajeDescuento, Activo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@EventoId, @ProvinciaCodigo, @AplicaPrecioSocio, @PorcentajeDescuento, @Activo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(EventoProvinciaBeneficio entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE EventoProvinciaBeneficios
            SET EventoId = @EventoId, ProvinciaCodigo = @ProvinciaCodigo, AplicaPrecioSocio = @AplicaPrecioSocio,
                PorcentajeDescuento = @PorcentajeDescuento, Activo = @Activo,
                UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE EventoProvinciaBeneficios SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
