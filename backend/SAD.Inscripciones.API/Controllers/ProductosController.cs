using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private const long MaxImagenBytes = 2 * 1024 * 1024; // 2 MB

    // No SVG (served same-origin) and no GIF.
    private static readonly Dictionary<string, string[]> AllowedImagenTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = new[] { ".png" },
        ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
        ["image/webp"] = new[] { ".webp" },
    };

    private readonly IProductoService _service;

    public ProductosController(IProductoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetPublicosAsync());
    }

    [HttpGet("admin")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetAllAdmin()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("admin/{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetByIdAdmin(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetPublicoByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create([FromBody] ProductoCreateDto dto)
    {
        var id = await _service.CreateAsync(dto);
        var creado = await _service.GetByIdAsync(id);
        return CreatedAtAction(nameof(GetByIdAdmin), new { id }, creado);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpGet("{id}/imagen")]
    public async Task<IActionResult> GetImagen(int id)
    {
        var imagen = await _service.GetImagenAsync(id);
        // The ?v= query param on the URL changes with every replacement, so the file can be cached forever.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(imagen.Contenido, imagen.ContentType);
    }

    [HttpPut("admin/{id}/imagen")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> SetImagen(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Archivo vacío." });
        if (file.Length > MaxImagenBytes)
            return BadRequest(new { message = $"La imagen excede el máximo de {MaxImagenBytes / 1024 / 1024} MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImagenTypes.TryGetValue(file.ContentType, out var extensiones) || !extensiones.Contains(ext))
            return BadRequest(new { message = "Tipo de archivo no permitido. Use PNG, JPG o WebP." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        await _service.SetImagenAsync(id, ms.ToArray(), file.ContentType.ToLowerInvariant());
        return NoContent();
    }

    [HttpDelete("admin/{id}/imagen")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeleteImagen(int id)
    {
        await _service.DeleteImagenAsync(id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await _service.DeleteAsync(id);
        return Ok(new { deleted = resultado.ToString() });
    }
}
