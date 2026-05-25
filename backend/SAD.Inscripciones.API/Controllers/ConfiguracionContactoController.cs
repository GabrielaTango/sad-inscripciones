using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/admin/configuracion-contacto")]
[Authorize(Policy = "Admin")]
public class ConfiguracionContactoController : ControllerBase
{
    private readonly IConfiguracionContactoRepository _repo;
    private readonly IEmailService _emailService;

    public ConfiguracionContactoController(
        IConfiguracionContactoRepository repo,
        IEmailService emailService)
    {
        _repo = repo;
        _emailService = emailService;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "admin";

    [HttpGet]
    public async Task<ActionResult<ConfiguracionContactoDto>> Get()
    {
        var c = await _repo.GetAsync();
        return Ok(new ConfiguracionContactoDto
        {
            EmailDestino = c.EmailDestino,
            Activo = c.Activo,
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy,
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ConfiguracionContactoUpdateDto dto)
    {
        await _repo.UpdateAsync(new ConfiguracionContacto
        {
            Id = 1,
            EmailDestino = dto.EmailDestino?.Trim() ?? string.Empty,
            Activo = dto.Activo,
            UpdatedBy = GetCurrentUser(),
        });

        _emailService.InvalidarCacheContacto();
        return NoContent();
    }
}
