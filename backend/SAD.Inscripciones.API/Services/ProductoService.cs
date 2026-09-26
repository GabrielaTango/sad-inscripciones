using System.Text.Json;
using System.Text.RegularExpressions;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class ProductoService : IProductoService
{
    private static readonly string[] TiposValidos = { "text", "number", "select" };
    private static readonly string[] ClavesReservadas = { "dni", "nombre", "apellido", "email" };
    private static readonly Regex ClaveInvalidaRegex = new("[^a-z0-9_]", RegexOptions.Compiled);

    private readonly IProductoRepository _repository;

    public ProductoService(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProductoPublicoDto>> GetPublicosAsync()
    {
        var productos = await _repository.GetAllAsync(soloActivos: true);
        return productos.Select(ToPublicoDto);
    }

    public async Task<ProductoPublicoDto> GetPublicoByIdAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto == null || !producto.Activo)
            throw new NotFoundException("Producto", id);

        return ToPublicoDto(producto);
    }

    public async Task<IEnumerable<ProductoDto>> GetAllAsync()
    {
        var productos = await _repository.GetAllAsync(soloActivos: false);
        return productos.Select(ToDto);
    }

    public async Task<ProductoDto> GetByIdAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id) ?? throw new NotFoundException("Producto", id);
        return ToDto(producto);
    }

    public async Task<int> CreateAsync(ProductoCreateDto dto)
    {
        var camposExtra = ValidarYNormalizar(dto.Nombre, dto.Precio, dto.CamposExtra);

        var entity = new Producto
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion,
            Precio = dto.Precio,
            Activo = dto.Activo,
            ImagenUrl = dto.ImagenUrl,
            CamposExtra = JsonSerializer.Serialize(camposExtra),
            MailAsunto = dto.MailAsunto,
            MailCuerpoHtml = dto.MailCuerpoHtml
        };

        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(int id, ProductoUpdateDto dto)
    {
        var existente = await _repository.GetByIdAsync(id) ?? throw new NotFoundException("Producto", id);
        var camposExtra = ValidarYNormalizar(dto.Nombre, dto.Precio, dto.CamposExtra);

        existente.Nombre = dto.Nombre.Trim();
        existente.Descripcion = dto.Descripcion;
        existente.Precio = dto.Precio;
        existente.Activo = dto.Activo;
        existente.ImagenUrl = dto.ImagenUrl;
        existente.CamposExtra = JsonSerializer.Serialize(camposExtra);
        existente.MailAsunto = dto.MailAsunto;
        existente.MailCuerpoHtml = dto.MailCuerpoHtml;

        await _repository.UpdateAsync(existente);
    }

    public async Task<ProductoDeleteResult> DeleteAsync(int id)
    {
        var resultado = await _repository.DeleteAsync(id);
        if (resultado == ProductoDeleteResult.NotFound)
            throw new NotFoundException("Producto", id);

        return resultado;
    }

    /// <summary>
    /// Validates the product's basic data and its CamposExtra schema, normalizing
    /// each key (lowercase, letters/digits/underscore only) and rejecting reserved
    /// keys that collide with the buyer's own fields (dni, nombre, apellido, email).
    /// </summary>
    private static List<CampoExtraProducto> ValidarYNormalizar(string nombre, decimal precio, List<CampoExtraProducto> camposExtra)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new BusinessException("El nombre es obligatorio.");

        if (precio <= 0)
            throw new BusinessException("El precio debe ser mayor a cero.");

        var normalizados = new List<CampoExtraProducto>();
        var clavesUsadas = new HashSet<string>();

        foreach (var campo in camposExtra ?? new List<CampoExtraProducto>())
        {
            if (string.IsNullOrWhiteSpace(campo.Label))
                throw new BusinessException("Cada campo extra debe tener una etiqueta (label).");

            if (string.IsNullOrWhiteSpace(campo.Key))
                throw new BusinessException($"El campo \"{campo.Label}\" debe tener una clave (key).");

            var clave = ClaveInvalidaRegex.Replace(campo.Key.Trim().ToLowerInvariant(), string.Empty);
            if (string.IsNullOrEmpty(clave))
                throw new BusinessException($"La clave del campo \"{campo.Label}\" no es válida: solo se permiten letras, dígitos y guión bajo.");

            if (ClavesReservadas.Contains(clave))
                throw new BusinessException($"La clave \"{clave}\" está reservada y no puede usarse en un campo extra.");

            if (!clavesUsadas.Add(clave))
                throw new BusinessException($"La clave \"{clave}\" está repetida entre los campos extra.");

            var tipo = campo.Type?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!TiposValidos.Contains(tipo))
                throw new BusinessException($"El tipo \"{campo.Type}\" no es válido para el campo \"{clave}\". Debe ser text, number o select.");

            var opciones = (campo.Options ?? new List<string>())
                .Select(o => o?.Trim() ?? string.Empty)
                .Where(o => !string.IsNullOrEmpty(o))
                .ToList();

            if (tipo == "select" && opciones.Count == 0)
                throw new BusinessException($"El campo \"{clave}\" es de tipo select y requiere al menos una opción.");

            normalizados.Add(new CampoExtraProducto
            {
                Key = clave,
                Label = campo.Label.Trim(),
                Type = tipo,
                Options = tipo == "select" ? opciones : new List<string>(),
                Required = campo.Required
            });
        }

        return normalizados;
    }

    private static ProductoDto ToDto(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        Descripcion = producto.Descripcion,
        Precio = producto.Precio,
        Activo = producto.Activo,
        ImagenUrl = producto.ImagenUrl,
        CamposExtra = DeserializeCamposExtra(producto.CamposExtra),
        MailAsunto = producto.MailAsunto,
        MailCuerpoHtml = producto.MailCuerpoHtml,
        FechaAlta = producto.FechaAlta
    };

    private static ProductoPublicoDto ToPublicoDto(Producto producto) => new()
    {
        Id = producto.Id,
        Nombre = producto.Nombre,
        Descripcion = producto.Descripcion,
        Precio = producto.Precio,
        ImagenUrl = producto.ImagenUrl,
        CamposExtra = DeserializeCamposExtra(producto.CamposExtra)
    };

    private static List<CampoExtraProducto> DeserializeCamposExtra(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<CampoExtraProducto>();

        return JsonSerializer.Deserialize<List<CampoExtraProducto>>(json) ?? new List<CampoExtraProducto>();
    }
}
