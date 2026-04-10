using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class TipoEventoRepository : ITipoEventoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public TipoEventoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<TipoEvento>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<TipoEvento>(
            "SELECT * FROM TiposEvento WHERE DeletedAt IS NULL ORDER BY Nombre");
    }

    public async Task<TipoEvento?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TipoEvento>(
            "SELECT * FROM TiposEvento WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<TipoEvento?> GetByNombreAsync(string nombre)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TipoEvento>(
            "SELECT * FROM TiposEvento WHERE Nombre = @Nombre AND DeletedAt IS NULL", new { Nombre = nombre });
    }

    public async Task<int> CreateAsync(TipoEvento entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO TiposEvento (Nombre, Activo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@Nombre, @Activo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(TipoEvento entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE TiposEvento
            SET Nombre = @Nombre, Activo = @Activo, UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE TiposEvento SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
