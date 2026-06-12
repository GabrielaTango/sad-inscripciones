using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class EventoPrecioRepository : IEventoPrecioRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public EventoPrecioRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<EventoPrecio>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<EventoPrecio>(
            "SELECT * FROM EventoPrecios WHERE DeletedAt IS NULL");
    }

    public async Task<EventoPrecio?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EventoPrecio>(
            "SELECT * FROM EventoPrecios WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<EventoPrecio>> GetByEventoIdAsync(int eventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<EventoPrecio>(
            "SELECT * FROM EventoPrecios WHERE EventoId = @EventoId AND DeletedAt IS NULL",
            new { EventoId = eventoId });
    }

    public async Task<EventoPrecio?> GetByEventoAndTipoAlumnoAsync(int eventoId, int tipoAlumnoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EventoPrecio>(
            @"SELECT * FROM EventoPrecios
              WHERE EventoId = @EventoId AND TipoAlumnoId = @TipoAlumnoId AND DeletedAt IS NULL AND Activo = 1",
            new { EventoId = eventoId, TipoAlumnoId = tipoAlumnoId });
    }

    public async Task<int> CreateAsync(EventoPrecio entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO EventoPrecios (EventoId, TipoAlumnoId, ArticuloCodigo, PrecioBase, PrecioDolares, PrecioCuotas, CantidadCuotas, PermiteDescuento, Activo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@EventoId, @TipoAlumnoId, @ArticuloCodigo, @PrecioBase, @PrecioDolares, @PrecioCuotas, @CantidadCuotas, @PermiteDescuento, @Activo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(EventoPrecio entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE EventoPrecios
            SET EventoId = @EventoId, TipoAlumnoId = @TipoAlumnoId, ArticuloCodigo = @ArticuloCodigo,
                PrecioBase = @PrecioBase, PrecioDolares = @PrecioDolares, PrecioCuotas = @PrecioCuotas, CantidadCuotas = @CantidadCuotas,
                PermiteDescuento = @PermiteDescuento, Activo = @Activo,
                UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE EventoPrecios SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
