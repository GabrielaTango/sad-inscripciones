using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
public class ConfiguracionPayPalController : ControllerBase
{
    private readonly IConfiguracionPayPalRepository _repo;

    public ConfiguracionPayPalController(IConfiguracionPayPalRepository repo)
    {
        _repo = repo;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "admin";

    /// <summary>Config completa para el panel de administración.</summary>
    [HttpGet("api/admin/configuracion-paypal")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ConfiguracionPayPalDto>> Get()
    {
        var c = await _repo.GetAsync();
        return Ok(new ConfiguracionPayPalDto
        {
            ClientId = c.ClientId,
            Moneda = c.Moneda,
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy,
        });
    }

    [HttpPut("api/admin/configuracion-paypal")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update([FromBody] ConfiguracionPayPalUpdateDto dto)
    {
        await _repo.UpdateAsync(new ConfiguracionPayPal
        {
            Id = 1,
            ClientId = dto.ClientId?.Trim() ?? string.Empty,
            Moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? "USD" : dto.Moneda.Trim().ToUpperInvariant(),
            UpdatedBy = GetCurrentUser(),
        });
        return NoContent();
    }

    /// <summary>
    /// Config pública que el frontend usa para cargar el SDK de PayPal en el navegador.
    /// El Client-ID no es secreto.
    /// </summary>
    [HttpGet("api/configuracion-paypal/public")]
    [AllowAnonymous]
    public async Task<ActionResult<ConfiguracionPayPalPublicDto>> GetPublic()
    {
        var c = await _repo.GetAsync();
        return Ok(new ConfiguracionPayPalPublicDto
        {
            ClientId = c.ClientId,
            Moneda = c.Moneda,
        });
    }
}
