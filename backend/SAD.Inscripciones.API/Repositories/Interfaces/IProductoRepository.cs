using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

/// <summary>Result of <see cref="IProductoRepository.DeleteAsync"/>.</summary>
public enum ProductoDeleteResult
{
    NotFound,

    /// <summary>Hard-deleted: no VentasProducto rows reference it.</summary>
    Deleted,

    /// <summary>Soft-deleted (Activo = 0): kept because sales reference it via the FK.</summary>
    Deactivated
}

public interface IProductoRepository
{
    Task<IEnumerable<Producto>> GetAllAsync(bool soloActivos);
    Task<Producto?> GetByIdAsync(int id);
    Task<int> CreateAsync(Producto entity);
    Task<bool> UpdateAsync(Producto entity);

    /// <summary>
    /// Hard-deletes the product when no VentasProducto rows reference it (the FK
    /// has no ON DELETE CASCADE, so a hard delete would otherwise fail); when
    /// sales exist, soft-deletes it instead by setting Activo = 0.
    /// </summary>
    Task<ProductoDeleteResult> DeleteAsync(int id);

    /// <summary>Image of a product including its bytes, or null when it has none.</summary>
    Task<ProductoImagen?> GetImagenAsync(int productoId);

    /// <summary>Inserts or replaces the product's image (one per product).</summary>
    Task UpsertImagenAsync(int productoId, byte[] contenido, string contentType);

    Task<bool> DeleteImagenAsync(int productoId);
}
