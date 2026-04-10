using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/promocioncupones")]
[Authorize(Policy = "Admin")]
public class PromocionCuponesController : ControllerBase
{
    private readonly IPromocionCuponService _service;

    public PromocionCuponesController(IPromocionCuponService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? promocionId, [FromQuery] string? documento)
    {
        if (promocionId.HasValue)
            return Ok(await _service.GetByPromocionIdAsync(promocionId.Value));
        if (!string.IsNullOrEmpty(documento))
            return Ok(await _service.GetByDocumentoAsync(documento));
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [AllowAnonymous]
    [HttpGet("disponibles/{documento}")]
    public async Task<IActionResult> GetDisponibles(string documento)
    {
        var cupones = await _service.GetDisponiblesByDocumentoAsync(documento);
        return Ok(cupones);
    }

    [AllowAnonymous]
    [HttpGet("validar/{codigo}")]
    public async Task<IActionResult> Validar(string codigo)
    {
        var cupon = await _service.GetByCodigoAsync(codigo);

        if (cupon == null)
            return NotFound(new { valido = false, mensaje = "Cupón no encontrado." });

        if (cupon.Usado)
            return BadRequest(new { valido = false, mensaje = "El cupón ya fue utilizado." });

        if (cupon.FechaVencimiento.HasValue && DateTime.UtcNow > cupon.FechaVencimiento.Value)
            return BadRequest(new { valido = false, mensaje = "El cupón ha vencido." });

        return Ok(new
        {
            valido = true,
            promocionId = cupon.PromocionId,
            tipoDescuento = cupon.TipoDescuento,
            valor = cupon.Valor,
            acumulable = cupon.Acumulable,
            documento = cupon.Documento
        });
    }
}
