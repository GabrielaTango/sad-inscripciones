using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/tiposalumno")]
public class TiposAlumnoController : ControllerBase
{
    private readonly ITipoAlumnoService _service;

    public TiposAlumnoController(ITipoAlumnoService service)
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
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create([FromBody] TipoAlumnoDto dto)
    {
        var entity = new TipoAlumno
        {
            Nombre = dto.Nombre,
            Activo = dto.Activo,
            Extranjero = dto.Extranjero,
            CreatedBy = GetCurrentUser(),
            UpdatedBy = GetCurrentUser()
        };
        var id = await _service.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, entity);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] TipoAlumnoDto dto)
    {
        var entity = new TipoAlumno
        {
            Id = id,
            Nombre = dto.Nombre,
            Activo = dto.Activo,
            Extranjero = dto.Extranjero,
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
