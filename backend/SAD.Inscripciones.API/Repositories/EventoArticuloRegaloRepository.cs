using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class EventoArticuloRegaloRepository : IEventoArticuloRegaloRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public EventoArticuloRegaloRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<EventoArticuloRegalo>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<EventoArticuloRegalo>(
            "SELECT * FROM EventoArticuloRegalos WHERE DeletedAt IS NULL");
    }

    public async Task<EventoArticuloRegalo?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EventoArticuloRegalo>(
            "SELECT * FROM EventoArticuloRegalos WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<EventoArticuloRegalo>> GetByEventoIdAsync(int eventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<EventoArticuloRegalo>(
            "SELECT * FROM EventoArticuloRegalos WHERE EventoId = @EventoId AND DeletedAt IS NULL",
            new { EventoId = eventoId });
    }

    public async Task<int> CreateAsync(EventoArticuloRegalo entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO EventoArticuloRegalos (EventoId, TipoAlumnoId, ArticuloCodigo, DescripcionArticulo, Cantidad, CondicionEspecial, Activo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@EventoId, @TipoAlumnoId, @ArticuloCodigo, @DescripcionArticulo, @Cantidad, @CondicionEspecial, @Activo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(EventoArticuloRegalo entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE EventoArticuloRegalos
            SET EventoId = @EventoId, TipoAlumnoId = @TipoAlumnoId, ArticuloCodigo = @ArticuloCodigo,
                DescripcionArticulo = @DescripcionArticulo, Cantidad = @Cantidad, CondicionEspecial = @CondicionEspecial,
                Activo = @Activo, UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE EventoArticuloRegalos SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
