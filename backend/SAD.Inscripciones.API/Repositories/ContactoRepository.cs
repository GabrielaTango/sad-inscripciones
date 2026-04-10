using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class ContactoRepository : IContactoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public ContactoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Contacto>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Contacto>(
            "SELECT * FROM Contactos ORDER BY FechaEnvio DESC");
    }

    public async Task<Contacto?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Contacto>(
            "SELECT * FROM Contactos WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Contacto contacto)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO Contactos (Nombre, Email, Asunto, Mensaje, FechaEnvio, Leido)
            VALUES (@Nombre, @Email, @Asunto, @Mensaje, @FechaEnvio, @Leido);
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, contacto);
    }

    public async Task<bool> UpdateAsync(Contacto contacto)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Contactos
            SET Nombre = @Nombre, Email = @Email, Asunto = @Asunto,
                Mensaje = @Mensaje, Leido = @Leido
            WHERE Id = @Id";
        var rows = await connection.ExecuteAsync(sql, contacto);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        var rows = await connection.ExecuteAsync(
            "DELETE FROM Contactos WHERE Id = @Id", new { Id = id });
        return rows > 0;
    }
}
