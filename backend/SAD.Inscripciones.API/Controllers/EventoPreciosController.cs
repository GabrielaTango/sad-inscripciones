using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/eventoprecios")]
public class EventoPreciosController : ControllerBase
{
    private readonly IEventoPrecioService _service;

    public EventoPreciosController(IEventoPrecioService service)
    {
        _service = service;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "system";

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? eventoId)
    {
        if (eventoId.HasValue)
            return Ok(await _service.GetByEventoIdAsync(eventoId.Value));
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create([FromBody] EventoPrecioDto dto)
    {
        var entity = new EventoPrecio
        {
            EventoId = dto.EventoId,
            TipoAlumnoId = dto.TipoAlumnoId,
            ArticuloCodigo = dto.ArticuloCodigo,
            PrecioBase = dto.PrecioBase,
            PrecioDolares = dto.PrecioDolares,
            PrecioCuotas = dto.PrecioCuotas,
            CantidadCuotas = dto.CantidadCuotas,
            PermiteDescuento = dto.PermiteDescuento,
            Activo = dto.Activo,
            CreatedBy = GetCurrentUser(),
            UpdatedBy = GetCurrentUser()
        };
        var id = await _service.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, entity);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] EventoPrecioDto dto)
    {
        var entity = new EventoPrecio
        {
            Id = id,
            EventoId = dto.EventoId,
            TipoAlumnoId = dto.TipoAlumnoId,
            ArticuloCodigo = dto.ArticuloCodigo,
            PrecioBase = dto.PrecioBase,
            PrecioDolares = dto.PrecioDolares,
            PrecioCuotas = dto.PrecioCuotas,
            CantidadCuotas = dto.CantidadCuotas,
            PermiteDescuento = dto.PermiteDescuento,
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
}
