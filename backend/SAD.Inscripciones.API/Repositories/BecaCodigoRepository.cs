using System.Data;
using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class BecaCodigoRepository : IBecaCodigoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public BecaCodigoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<BecaCodigo>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<BecaCodigo>(
            "SELECT * FROM BecaCodigos WHERE DeletedAt IS NULL ORDER BY CreatedAt DESC");
    }

    public async Task<BecaCodigo?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<BecaCodigo>(
            "SELECT * FROM BecaCodigos WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<BecaCodigo>> GetByBecaEventoIdAsync(int becaEventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<BecaCodigo>(
            "SELECT * FROM BecaCodigos WHERE BecaEventoId = @BecaEventoId AND DeletedAt IS NULL",
            new { BecaEventoId = becaEventoId });
    }

    public async Task<BecaCodigo?> GetByCodigoAsync(string codigo)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<BecaCodigo>(
            "SELECT * FROM BecaCodigos WHERE Codigo = @Codigo AND DeletedAt IS NULL",
            new { Codigo = codigo });
    }

    public async Task<int> CreateAsync(BecaCodigo entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO BecaCodigos (BecaEventoId, Codigo, Usado, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@BecaEventoId, @Codigo, 0, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task CreateBatchAsync(IEnumerable<BecaCodigo> entities)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO BecaCodigos (BecaEventoId, Codigo, Usado, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@BecaEventoId, @Codigo, 0, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP())";
        await connection.ExecuteAsync(sql, entities);
    }

    public async Task<bool> MarcarUsadoAsync(string codigo, int inscripcionId, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = @"
            UPDATE BecaCodigos
            SET Usado = 1, FechaUso = UTC_TIMESTAMP(), InscripcionId = @InscripcionId, UpdatedAt = UTC_TIMESTAMP()
            WHERE Codigo = @Codigo AND Usado = 0 AND DeletedAt IS NULL";
        var rows = await connection.ExecuteAsync(sql, new { Codigo = codigo, InscripcionId = inscripcionId }, transaction);
        return rows > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE BecaCodigos SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }

    public async Task<IEnumerable<dynamic>> GetCodigosExportAsync(int becaEventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            SELECT bc.Id, bc.Codigo, bc.Usado, bc.FechaUso, bc.InscripcionId,
                   be.NombreCampana, be.TipoDescuento, be.Valor, be.FechaVencimiento,
                   e.Titulo AS NombreEvento
            FROM BecaCodigos bc
            INNER JOIN BecaEventos be ON bc.BecaEventoId = be.Id
            INNER JOIN Eventos e ON be.EventoId = e.Id
            WHERE bc.BecaEventoId = @BecaEventoId AND bc.DeletedAt IS NULL
            ORDER BY bc.Id";
        return await connection.QueryAsync(sql, new { BecaEventoId = becaEventoId });
    }
}
