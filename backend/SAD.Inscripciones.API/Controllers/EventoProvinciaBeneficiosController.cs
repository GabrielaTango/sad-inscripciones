using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/eventoprovinciabeneficios")]
[Authorize(Policy = "Admin")]
public class EventoProvinciaBeneficiosController : ControllerBase
{
    private readonly IEventoProvinciaBeneficioService _service;

    public EventoProvinciaBeneficiosController(IEventoProvinciaBeneficioService service)
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
    public async Task<IActionResult> Create([FromBody] EventoProvinciaBeneficioDto dto)
    {
        var entity = new EventoProvinciaBeneficio
        {
            EventoId = dto.EventoId,
            ProvinciaCodigo = dto.ProvinciaCodigo,
            AplicaPrecioSocio = dto.AplicaPrecioSocio,
            PorcentajeDescuento = dto.PorcentajeDescuento,
            Activo = dto.Activo,
            CreatedBy = GetCurrentUser(),
            UpdatedBy = GetCurrentUser()
        };
        var id = await _service.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] EventoProvinciaBeneficioDto dto)
    {
        var entity = new EventoProvinciaBeneficio
        {
            Id = id,
            EventoId = dto.EventoId,
            ProvinciaCodigo = dto.ProvinciaCodigo,
            AplicaPrecioSocio = dto.AplicaPrecioSocio,
            PorcentajeDescuento = dto.PorcentajeDescuento,
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
