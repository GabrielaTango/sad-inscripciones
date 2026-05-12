using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/admin/configuracion-mercadopago")]
[Authorize(Policy = "Admin")]
public class ConfiguracionMercadoPagoController : ControllerBase
{
    private readonly IConfiguracionMercadoPagoRepository _repo;
    private readonly ICryptoService _crypto;
    private readonly IMercadoPagoService _mpService;

    public ConfiguracionMercadoPagoController(
        IConfiguracionMercadoPagoRepository repo,
        ICryptoService crypto,
        IMercadoPagoService mpService)
    {
        _repo = repo;
        _crypto = crypto;
        _mpService = mpService;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "admin";

    [HttpGet]
    public async Task<ActionResult<ConfiguracionMercadoPagoDto>> Get()
    {
        var c = await _repo.GetAsync();

        string? preview = null;
        if (!string.IsNullOrEmpty(c.AccessTokenCifrado))
        {
            try
            {
                var plain = _crypto.Decrypt(c.AccessTokenCifrado);
                if (!string.IsNullOrEmpty(plain))
                    preview = plain.Length <= 12 ? plain : plain[..12] + "...";
            }
            catch
            {
                preview = "(error al descifrar)";
            }
        }

        return Ok(new ConfiguracionMercadoPagoDto
        {
            FrontendBaseUrl = c.FrontendBaseUrl,
            TieneAccessToken = !string.IsNullOrEmpty(c.AccessTokenCifrado),
            AccessTokenPreview = preview,
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy,
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ConfiguracionMercadoPagoUpdateDto dto)
    {
        var current = await _repo.GetAsync();

        var accessTokenCifrado = string.IsNullOrWhiteSpace(dto.AccessToken)
            ? current.AccessTokenCifrado
            : _crypto.Encrypt(dto.AccessToken.Trim());

        await _repo.UpdateAsync(new ConfiguracionMercadoPago
        {
            Id = 1,
            AccessTokenCifrado = accessTokenCifrado,
            FrontendBaseUrl = dto.FrontendBaseUrl?.Trim() ?? string.Empty,
            UpdatedBy = GetCurrentUser(),
        });

        _mpService.InvalidarCache();
        return NoContent();
    }
}
