using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/eventoarticuloregalos")]
[Authorize(Policy = "Admin")]
public class EventoArticuloRegalosController : ControllerBase
{
    private readonly IEventoArticuloRegaloService _service;

    public EventoArticuloRegalosController(IEventoArticuloRegaloService service)
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
    public async Task<IActionResult> Create([FromBody] EventoArticuloRegaloDto dto)
    {
        var entity = new EventoArticuloRegalo
        {
            EventoId = dto.EventoId,
            TipoAlumnoId = dto.TipoAlumnoId,
            ArticuloCodigo = dto.ArticuloCodigo,
            DescripcionArticulo = dto.DescripcionArticulo,
            Cantidad = dto.Cantidad,
            CondicionEspecial = dto.CondicionEspecial,
            Activo = dto.Activo,
            CreatedBy = GetCurrentUser(),
            UpdatedBy = GetCurrentUser()
        };
        var id = await _service.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] EventoArticuloRegaloDto dto)
    {
        var entity = new EventoArticuloRegalo
        {
            Id = id,
            EventoId = dto.EventoId,
            TipoAlumnoId = dto.TipoAlumnoId,
            ArticuloCodigo = dto.ArticuloCodigo,
            DescripcionArticulo = dto.DescripcionArticulo,
            Cantidad = dto.Cantidad,
            CondicionEspecial = dto.CondicionEspecial,
            Activo = dto.Activo,
            UpdatedBy = GetCurrentUser()
        };
        await _service.UpdateAsync(entity);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, GetCurrentUser());
        return NoContent();
    }
}
