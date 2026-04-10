using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/pagos")]
[Authorize(Policy = "Admin")]
public class PagosController : ControllerBase
{
    private readonly IPagoService _service;

    public PagosController(IPagoService service)
    {
        _service = service;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "system";

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? inscripcionId)
    {
        if (inscripcionId.HasValue)
            return Ok(await _service.GetByInscripcionIdAsync(inscripcionId.Value));
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PagoDto dto)
    {
        var entity = new Pago
        {
            InscripcionId = dto.InscripcionId,
            MedioPago = dto.MedioPago,
            EstadoPago = "Pendiente",
            Monto = dto.Monto,
            ReferenciaExterna = dto.ReferenciaExterna,
            FechaPago = dto.FechaPago,
            Observaciones = dto.Observaciones,
            CreatedBy = GetCurrentUser(),
            UpdatedBy = GetCurrentUser()
        };
        var id = await _service.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, entity);
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] PagoUpdateEstadoDto dto)
    {
        await _service.UpdateEstadoAsync(id, dto.EstadoPago, GetCurrentUser());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, GetCurrentUser());
        return NoContent();
    }
}
