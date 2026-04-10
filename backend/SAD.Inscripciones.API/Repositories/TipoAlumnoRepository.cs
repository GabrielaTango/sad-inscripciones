using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class TipoAlumnoRepository : ITipoAlumnoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public TipoAlumnoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<TipoAlumno>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<TipoAlumno>(
            "SELECT * FROM TiposAlumno WHERE DeletedAt IS NULL ORDER BY Nombre");
    }

    public async Task<TipoAlumno?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TipoAlumno>(
            "SELECT * FROM TiposAlumno WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<TipoAlumno?> GetByNombreAsync(string nombre)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TipoAlumno>(
            "SELECT * FROM TiposAlumno WHERE Nombre = @Nombre AND DeletedAt IS NULL", new { Nombre = nombre });
    }

    public async Task<int> CreateAsync(TipoAlumno entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO TiposAlumno (Nombre, Activo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@Nombre, @Activo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(TipoAlumno entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE TiposAlumno
            SET Nombre = @Nombre, Activo = @Activo, UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE TiposAlumno SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
