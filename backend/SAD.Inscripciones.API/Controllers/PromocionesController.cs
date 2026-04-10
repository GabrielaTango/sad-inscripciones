using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/promociones")]
[Authorize(Policy = "Admin")]
public class PromocionesController : ControllerBase
{
    private readonly IPromocionService _service;

    public PromocionesController(IPromocionService service)
    {
        _service = service;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "system";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PromocionDto dto)
    {
        var entity = new Promocion
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            TipoAlumnoId = dto.TipoAlumnoId,
            CantidadCursosRequeridos = dto.CantidadCursosRequeridos,
            PeriodoMeses = dto.PeriodoMeses,
            TipoDescuento = dto.TipoDescuento,
            Valor = dto.Valor,
            Acumulable = dto.Acumulable,
            FechaVigenciaDesde = dto.FechaVigenciaDesde,
            FechaVigenciaHasta = dto.FechaVigenciaHasta,
            DiasValidezCupon = dto.DiasValidezCupon,
            Activo = dto.Activo
        };
        var id = await _service.CreateAsync(entity, GetCurrentUser());
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PromocionDto dto)
    {
        var entity = new Promocion
        {
            Id = id,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            TipoAlumnoId = dto.TipoAlumnoId,
            CantidadCursosRequeridos = dto.CantidadCursosRequeridos,
            PeriodoMeses = dto.PeriodoMeses,
            TipoDescuento = dto.TipoDescuento,
            Valor = dto.Valor,
            Acumulable = dto.Acumulable,
            FechaVigenciaDesde = dto.FechaVigenciaDesde,
            FechaVigenciaHasta = dto.FechaVigenciaHasta,
            DiasValidezCupon = dto.DiasValidezCupon,
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
