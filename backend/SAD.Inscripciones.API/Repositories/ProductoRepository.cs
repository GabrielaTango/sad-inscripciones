using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public ProductoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Producto>> GetAllAsync(bool soloActivos)
    {
        using var connection = _dbFactory.CreateConnection();
        var sql = soloActivos
            ? "SELECT * FROM Productos WHERE Activo = 1 ORDER BY Id DESC"
            : "SELECT * FROM Productos ORDER BY Id DESC";
        return await connection.QueryAsync<Producto>(sql);
    }

    public async Task<Producto?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Producto>(
            "SELECT * FROM Productos WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Producto entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO Productos (Nombre, Descripcion, Precio, Activo, ImagenUrl, CamposExtra, MailAsunto, MailCuerpoHtml, FechaAlta, CreatedAt, UpdatedAt)
            VALUES (@Nombre, @Descripcion, @Precio, @Activo, @ImagenUrl, @CamposExtra, @MailAsunto, @MailCuerpoHtml, UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Producto entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Productos
            SET Nombre = @Nombre, Descripcion = @Descripcion, Precio = @Precio, Activo = @Activo,
                ImagenUrl = @ImagenUrl, CamposExtra = @CamposExtra, MailAsunto = @MailAsunto,
                MailCuerpoHtml = @MailCuerpoHtml, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<ProductoDeleteResult> DeleteAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        connection.Open();

        var existe = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Productos WHERE Id = @Id", new { Id = id });
        if (existe == 0)
        {
            return ProductoDeleteResult.NotFound;
        }

        var tieneVentas = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM VentasProducto WHERE ProductoId = @Id", new { Id = id });

        if (tieneVentas > 0)
        {
            await connection.ExecuteAsync(
                "UPDATE Productos SET Activo = 0, UpdatedAt = UTC_TIMESTAMP() WHERE Id = @Id", new { Id = id });
            return ProductoDeleteResult.Deactivated;
        }

        await connection.ExecuteAsync("DELETE FROM Productos WHERE Id = @Id", new { Id = id });
        return ProductoDeleteResult.Deleted;
    }
}
