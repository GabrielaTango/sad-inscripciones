using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class PagoRepository : IPagoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public PagoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Pago>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Pago>(
            "SELECT * FROM Pagos WHERE DeletedAt IS NULL ORDER BY CreatedAt DESC");
    }

    public async Task<Pago?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Pago>(
            "SELECT * FROM Pagos WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<Pago>> GetByInscripcionIdAsync(int inscripcionId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Pago>(
            "SELECT * FROM Pagos WHERE InscripcionId = @InscripcionId AND DeletedAt IS NULL",
            new { InscripcionId = inscripcionId });
    }

    public async Task<int> CreateAsync(Pago entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO Pagos (InscripcionId, MedioPago, EstadoPago, Monto, ReferenciaExterna, FechaPago, Observaciones, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@InscripcionId, @MedioPago, @EstadoPago, @Monto, @ReferenciaExterna, @FechaPago, @Observaciones, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Pago entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Pagos
            SET MedioPago = @MedioPago, EstadoPago = @EstadoPago, Monto = @Monto,
                ReferenciaExterna = @ReferenciaExterna, FechaPago = @FechaPago,
                Observaciones = @Observaciones, UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Pagos SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
