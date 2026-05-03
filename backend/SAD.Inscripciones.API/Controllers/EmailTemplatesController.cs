using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/admin/email-templates")]
[Authorize(Policy = "Admin")]
public class EmailTemplatesController : ControllerBase
{
    private readonly IEmailTemplateRepository _repo;
    private readonly IEmailService _emailService;

    public EmailTemplatesController(IEmailTemplateRepository repo, IEmailService emailService)
    {
        _repo = repo;
        _emailService = emailService;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "admin";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmailTemplateListItemDto>>> List()
    {
        var items = (await _repo.GetAllAsync())
            .Select(t => new EmailTemplateListItemDto
            {
                Codigo = t.Codigo,
                Nombre = t.Nombre,
                Activo = t.Activo,
                UpdatedAt = t.UpdatedAt,
            });
        return Ok(items);
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<EmailTemplateDto>> Get(string codigo)
    {
        var t = await _repo.GetByCodigoAsync(codigo);
        if (t == null) return NotFound();

        return Ok(new EmailTemplateDto
        {
            Codigo = t.Codigo,
            Nombre = t.Nombre,
            Asunto = t.Asunto,
            BodyHtml = t.BodyHtml,
            BodyJson = t.BodyJson,
            Activo = t.Activo,
            UpdatedAt = t.UpdatedAt,
            UpdatedBy = t.UpdatedBy,
        });
    }

    [HttpPut("{codigo}")]
    public async Task<IActionResult> Update(string codigo, [FromBody] EmailTemplateUpdateDto dto)
    {
        var existing = await _repo.GetByCodigoAsync(codigo);
        if (existing == null) return NotFound();

        existing.Asunto = dto.Asunto?.Trim() ?? string.Empty;
        existing.BodyHtml = dto.BodyHtml ?? string.Empty;
        existing.BodyJson = dto.BodyJson;
        existing.Activo = dto.Activo;
        existing.UpdatedBy = GetCurrentUser();

        await _repo.UpdateAsync(existing);
        return NoContent();
    }

    public class TemplateTestDto
    {
        public string Destinatario { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
    }

    [HttpPost("{codigo}/test")]
    public async Task<IActionResult> EnviarPrueba(string codigo, [FromBody] TemplateTestDto dto)
    {
        // codigo se acepta por consistencia URL pero el envío de prueba no lo necesita
        // (manda el HTML/asunto que vino en el body, sin tocar la DB).
        if (string.IsNullOrWhiteSpace(dto.Destinatario))
            return BadRequest(new { message = "Destinatario requerido." });

        try
        {
            await _emailService.EnviarPruebaTemplateAsync(dto.Destinatario, dto.Asunto, dto.BodyHtml);
            return Ok(new { message = "Mail de prueba enviado." });
        }
        catch (Exception ex)
        {
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
