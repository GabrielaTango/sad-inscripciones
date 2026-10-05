using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class ProductoRepository : IProductoRepository
{
    // Only UpdatedAt of the image is selected, never the blob.
    private const string SelectWithImagen = @"
        SELECT p.*, pi.UpdatedAt AS ImagenUpdatedAt
        FROM Productos p
        LEFT JOIN ProductoImagenes pi ON pi.ProductoId = p.Id";

    private readonly DbConnectionFactory _dbFactory;

    public ProductoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Producto>> GetAllAsync(bool soloActivos)
    {
        using var connection = _dbFactory.CreateConnection();
        var sql = soloActivos
            ? $"{SelectWithImagen} WHERE p.Activo = 1 ORDER BY p.Id DESC"
            : $"{SelectWithImagen} ORDER BY p.Id DESC";
        return await connection.QueryAsync<Producto>(sql);
    }

    public async Task<Producto?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Producto>(
            $"{SelectWithImagen} WHERE p.Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Producto entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO Productos (Nombre, Descripcion, Precio, Activo, CamposExtra, MailAsunto, MailCuerpoHtml, FechaAlta, CreatedAt, UpdatedAt)
            VALUES (@Nombre, @Descripcion, @Precio, @Activo, @CamposExtra, @MailAsunto, @MailCuerpoHtml, UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Producto entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Productos
            SET Nombre = @Nombre, Descripcion = @Descripcion, Precio = @Precio, Activo = @Activo,
                CamposExtra = @CamposExtra, MailAsunto = @MailAsunto,
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

        // ProductoImagenes rows go away with the product (ON DELETE CASCADE).
        await connection.ExecuteAsync("DELETE FROM Productos WHERE Id = @Id", new { Id = id });
        return ProductoDeleteResult.Deleted;
    }

    public async Task<ProductoImagen?> GetImagenAsync(int productoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProductoImagen>(
            "SELECT ProductoId, Contenido, ContentType, UpdatedAt FROM ProductoImagenes WHERE ProductoId = @ProductoId",
            new { ProductoId = productoId });
    }

    public async Task UpsertImagenAsync(int productoId, byte[] contenido, string contentType)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO ProductoImagenes (ProductoId, Contenido, ContentType, UpdatedAt)
            VALUES (@ProductoId, @Contenido, @ContentType, UTC_TIMESTAMP())
            ON DUPLICATE KEY UPDATE Contenido = VALUES(Contenido), ContentType = VALUES(ContentType), UpdatedAt = UTC_TIMESTAMP()";
        await connection.ExecuteAsync(sql, new { ProductoId = productoId, Contenido = contenido, ContentType = contentType });
    }

    public async Task<bool> DeleteImagenAsync(int productoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "DELETE FROM ProductoImagenes WHERE ProductoId = @ProductoId", new { ProductoId = productoId }) > 0;
    }
}
