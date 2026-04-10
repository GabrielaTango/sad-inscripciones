using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class BecaEventoRepository : IBecaEventoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public BecaEventoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<BecaEvento>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<BecaEvento>(
            "SELECT * FROM BecaEventos WHERE DeletedAt IS NULL ORDER BY CreatedAt DESC");
    }

    public async Task<BecaEvento?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<BecaEvento>(
            "SELECT * FROM BecaEventos WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<BecaEvento>> GetByEventoIdAsync(int eventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<BecaEvento>(
            "SELECT * FROM BecaEventos WHERE EventoId = @EventoId AND DeletedAt IS NULL",
            new { EventoId = eventoId });
    }

    public async Task<int> CreateAsync(BecaEvento entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO BecaEventos (EventoId, NombreCampana, TipoDescuento, Valor, CantidadTotalCodigos, FechaVencimiento, Acumulable, Activo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@EventoId, @NombreCampana, @TipoDescuento, @Valor, @CantidadTotalCodigos, @FechaVencimiento, @Acumulable, @Activo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(BecaEvento entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE BecaEventos
            SET EventoId = @EventoId, NombreCampana = @NombreCampana, TipoDescuento = @TipoDescuento,
                Valor = @Valor, CantidadTotalCodigos = @CantidadTotalCodigos, FechaVencimiento = @FechaVencimiento,
                Acumulable = @Acumulable, Activo = @Activo, UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE BecaEventos SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
