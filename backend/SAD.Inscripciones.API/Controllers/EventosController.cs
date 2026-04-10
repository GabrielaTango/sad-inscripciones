using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventosController : ControllerBase
{
    private readonly IEventoService _service;
    private readonly IEventoRepository _repository;
    private readonly IWebHostEnvironment _env;

    public EventosController(IEventoService service, IEventoRepository repository, IWebHostEnvironment env)
    {
        _service = service;
        _repository = repository;
        _env = env;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "system";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos()
    {
        return Ok(await _service.GetActivosAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create([FromBody] EventoCreateDto dto)
    {
        var entity = new Evento
        {
            TipoEventoId = dto.TipoEventoId,
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            FechaCierreInscripcion = dto.FechaCierreInscripcion,
            Lugar = dto.Lugar,
            Modalidad = dto.Modalidad,
            MaxInscriptos = dto.MaxInscriptos,
            Activo = true,
            CreatedBy = GetCurrentUser(),
            UpdatedBy = GetCurrentUser()
        };

        var id = await _service.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, entity);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] EventoUpdateDto dto)
    {
        var entity = new Evento
        {
            Id = id,
            TipoEventoId = dto.TipoEventoId,
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            FechaCierreInscripcion = dto.FechaCierreInscripcion,
            Lugar = dto.Lugar,
            Modalidad = dto.Modalidad,
            MaxInscriptos = dto.MaxInscriptos,
            Activo = dto.Activo,
            UpdatedBy = GetCurrentUser()
        };

        await _service.UpdateAsync(entity);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, GetCurrentUser());
        return NoContent();
    }

    [HttpGet("{id}/terminos/archivo")]
    public async Task<IActionResult> GetTerminosArchivo(int id)
    {
        var evento = await _service.GetByIdAsync(id);
        if (evento == null || string.IsNullOrEmpty(evento.TerminosArchivo))
            return NotFound(new { error = "Archivo no encontrado." });

        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "terminos");
        var filePath = Path.Combine(uploadsDir, evento.TerminosArchivo);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Archivo no encontrado en disco." });

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, "application/pdf", evento.TerminosArchivo);
    }

    [HttpPost("{id}/terminos")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> UploadTerminos(int id, IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { error = "Debe enviar un archivo." });

        var evento = await _service.GetByIdAsync(id);
        if (evento == null)
            return NotFound(new { error = "Evento no encontrado." });

        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "terminos");
        Directory.CreateDirectory(uploadsDir);

        // Borrar archivo anterior si existe
        if (!string.IsNullOrEmpty(evento.TerminosArchivo))
        {
            var oldPath = Path.Combine(uploadsDir, evento.TerminosArchivo);
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        var fileName = $"{id}_{Path.GetFileName(archivo.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        await _repository.UpdateTerminosArchivoAsync(id, fileName);
        return Ok(new { terminosArchivo = fileName });
    }

    [HttpDelete("{id}/terminos")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeleteTerminos(int id)
    {
        var evento = await _service.GetByIdAsync(id);
        if (evento == null)
            return NotFound(new { error = "Evento no encontrado." });

        if (!string.IsNullOrEmpty(evento.TerminosArchivo))
        {
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "terminos");
            var filePath = Path.Combine(uploadsDir, evento.TerminosArchivo);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        await _repository.UpdateTerminosArchivoAsync(id, null);
        return NoContent();
    }
}
