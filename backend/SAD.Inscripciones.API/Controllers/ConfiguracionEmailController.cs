using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/admin/configuracion-email")]
[Authorize(Policy = "Admin")]
public class ConfiguracionEmailController : ControllerBase
{
    private readonly IConfiguracionEmailRepository _repo;
    private readonly ICryptoService _crypto;
    private readonly IEmailService _emailService;

    public ConfiguracionEmailController(
        IConfiguracionEmailRepository repo,
        ICryptoService crypto,
        IEmailService emailService)
    {
        _repo = repo;
        _crypto = crypto;
        _emailService = emailService;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "admin";

    [HttpGet]
    public async Task<ActionResult<ConfiguracionEmailDto>> Get()
    {
        var c = await _repo.GetAsync();
        return Ok(new ConfiguracionEmailDto
        {
            Host = c.Host,
            Port = c.Port,
            EnableSsl = c.EnableSsl,
            Usuario = c.Usuario,
            FromEmail = c.FromEmail,
            FromName = c.FromName,
            ReplyTo = c.ReplyTo,
            BccCopia = c.BccCopia,
            Asunto = c.Asunto,
            Activo = c.Activo,
            IgnorarCertificadoSsl = c.IgnorarCertificadoSsl,
            TienePassword = !string.IsNullOrEmpty(c.PasswordCifrado),
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy,
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ConfiguracionEmailUpdateDto dto)
    {
        var current = await _repo.GetAsync();

        var passwordCifrado = string.IsNullOrEmpty(dto.Password)
            ? current.PasswordCifrado
            : _crypto.Encrypt(dto.Password);

        await _repo.UpdateAsync(new ConfiguracionEmail
        {
            Id = 1,
            Host = dto.Host?.Trim() ?? string.Empty,
            Port = dto.Port,
            EnableSsl = dto.EnableSsl,
            Usuario = dto.Usuario?.Trim() ?? string.Empty,
            PasswordCifrado = passwordCifrado,
            FromEmail = dto.FromEmail?.Trim() ?? string.Empty,
            FromName = dto.FromName?.Trim() ?? string.Empty,
            ReplyTo = string.IsNullOrWhiteSpace(dto.ReplyTo) ? null : dto.ReplyTo.Trim(),
            BccCopia = string.IsNullOrWhiteSpace(dto.BccCopia) ? null : dto.BccCopia.Trim(),
            Asunto = dto.Asunto?.Trim() ?? string.Empty,
            Activo = dto.Activo,
            IgnorarCertificadoSsl = dto.IgnorarCertificadoSsl,
            UpdatedBy = GetCurrentUser(),
        });

        _emailService.InvalidarCache();
        return NoContent();
    }

    [HttpPost("test")]
    public async Task<IActionResult> EnviarPrueba([FromBody] EmailTestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Destinatario))
            return BadRequest(new { message = "Destinatario requerido." });

        try
        {
            await _emailService.EnviarPruebaAsync(dto.Destinatario);
            return Ok(new { message = "Mail de prueba enviado." });
        }
        catch (Exception ex)
        {
            // Aplastamos la cadena de InnerException para devolver el detalle real
            // (SocketException, SslException, AuthenticationException, etc.)
            var partes = new List<string>();
            var cur = (Exception?)ex;
            while (cur != null)
            {
                partes.Add($"{cur.GetType().Name}: {cur.Message}");
                cur = cur.InnerException;
            }
            return BadRequest(new { message = string.Join(" -> ", partes) });
        }
    }
}
