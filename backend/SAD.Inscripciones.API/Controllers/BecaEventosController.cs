using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/becaeventos")]
[Authorize(Policy = "Admin")]
public class BecaEventosController : ControllerBase
{
    private readonly IBecaEventoService _service;

    public BecaEventosController(IBecaEventoService service)
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
    public async Task<IActionResult> Create([FromBody] BecaEventoDto dto)
    {
        var entity = new BecaEvento
        {
            EventoId = dto.EventoId,
            NombreCampana = dto.NombreCampana,
            TipoDescuento = dto.TipoDescuento,
            Valor = dto.Valor,
            CantidadTotalCodigos = dto.CantidadTotalCodigos,
            FechaVencimiento = dto.FechaVencimiento,
            Acumulable = dto.Acumulable,
            Activo = dto.Activo
        };
        var id = await _service.CreateAsync(entity, GetCurrentUser());
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BecaEventoDto dto)
    {
        var entity = new BecaEvento
        {
            Id = id,
            EventoId = dto.EventoId,
            NombreCampana = dto.NombreCampana,
            TipoDescuento = dto.TipoDescuento,
            Valor = dto.Valor,
            CantidadTotalCodigos = dto.CantidadTotalCodigos,
            FechaVencimiento = dto.FechaVencimiento,
            Acumulable = dto.Acumulable,
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
