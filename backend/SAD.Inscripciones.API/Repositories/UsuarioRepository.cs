using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public UsuarioRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Usuario>(
            "SELECT * FROM Usuarios WHERE DeletedAt IS NULL ORDER BY Username");
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Usuario>(
            "SELECT * FROM Usuarios WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<Usuario?> GetByUsernameAsync(string username)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Usuario>(
            "SELECT * FROM Usuarios WHERE Username = @Username AND DeletedAt IS NULL", new { Username = username });
    }

    public async Task<int> CreateAsync(Usuario entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO Usuarios (Username, PasswordHash, NombreCompleto, Email, Activo, CodVended, EsCapitulo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@Username, @PasswordHash, @NombreCompleto, @Email, @Activo, @CodVended, @EsCapitulo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Usuario entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Usuarios
            SET Username = @Username, NombreCompleto = @NombreCompleto, Email = @Email,
                Activo = @Activo, CodVended = @CodVended, EsCapitulo = @EsCapitulo,
                UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int id, string passwordHash, string updatedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Usuarios
            SET PasswordHash = @PasswordHash, UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, PasswordHash = passwordHash, UpdatedBy = updatedBy }) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Usuarios SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
