using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IProductoService
{
    /// <summary>Public catalog: only active products, without mail fields.</summary>
    Task<IEnumerable<ProductoPublicoDto>> GetPublicosAsync();

    /// <summary>Public detail: 404 if the product does not exist or is inactive.</summary>
    Task<ProductoPublicoDto> GetPublicoByIdAsync(int id);

    /// <summary>Admin listing: all products (active and inactive), full DTO.</summary>
    Task<IEnumerable<ProductoDto>> GetAllAsync();

    /// <summary>Admin detail: full DTO, including inactive products.</summary>
    Task<ProductoDto> GetByIdAsync(int id);

    Task<int> CreateAsync(ProductoCreateDto dto);
    Task UpdateAsync(int id, ProductoUpdateDto dto);
    Task<ProductoDeleteResult> DeleteAsync(int id);

    /// <summary>Public image bytes: 404 if the product has no image.</summary>
    Task<ProductoImagen> GetImagenAsync(int id);

    /// <summary>Stores or replaces the product's image: 404 if the product does not exist.</summary>
    Task SetImagenAsync(int id, byte[] contenido, string contentType);

    /// <summary>Removes the product's image: 404 if the product does not exist. No-op if it has none.</summary>
    Task DeleteImagenAsync(int id);
}
