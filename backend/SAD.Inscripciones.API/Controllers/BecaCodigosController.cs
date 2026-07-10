using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/becacodigos")]
[Authorize(Policy = "Admin")]
public class BecaCodigosController : ControllerBase
{
    private readonly IBecaCodigoService _service;
    private readonly IBecaEventoService _becaEventoService;

    public BecaCodigosController(IBecaCodigoService service, IBecaEventoService becaEventoService)
    {
        _service = service;
        _becaEventoService = becaEventoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? becaEventoId)
    {
        if (becaEventoId.HasValue)
            return Ok(await _service.GetByBecaEventoIdAsync(becaEventoId.Value));
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] int becaEventoId)
    {
        var bytes = await _service.ExportToExcelAsync(becaEventoId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BecaCodigos_{becaEventoId}.xlsx");
    }

    [HttpGet("validar/{codigo}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidarCodigo(string codigo)
    {
        var becaCodigo = await _service.GetByCodigoAsync(codigo);
        if (becaCodigo == null)
            return NotFound(new { error = "Código no encontrado." });
        if (becaCodigo.Usado)
            return BadRequest(new { error = "El código ya fue utilizado." });

        // Devolvemos el tipo/valor del descuento para que la pantalla de inscripción pueda
        // reflejarlo en el precio (necesario en el flujo en dólares / PayPal).
        var becaEvento = await _becaEventoService.GetByIdAsync(becaCodigo.BecaEventoId);
        return Ok(new
        {
            valido = true,
            becaEventoId = becaCodigo.BecaEventoId,
            eventoId = becaEvento.EventoId,
            tipoDescuento = becaEvento.TipoDescuento,
            valor = becaEvento.Valor,
        });
    }
}
