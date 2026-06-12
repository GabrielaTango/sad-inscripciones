using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Services;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InscripcionesController : ControllerBase
{
    private readonly IInscripcionService _service;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly IPayPalService _payPalService;
    private readonly IEventoService _eventoService;
    private readonly ITipoAlumnoService _tipoAlumnoService;
    private readonly IEventoPrecioService _eventoPrecioService;
    private readonly IInscripcionPagoValidationService _pagoValidation;
    private readonly ILogger<InscripcionesController> _logger;

    public InscripcionesController(
        IInscripcionService service,
        IMercadoPagoService mercadoPagoService,
        IPayPalService payPalService,
        IEventoService eventoService,
        ITipoAlumnoService tipoAlumnoService,
        IEventoPrecioService eventoPrecioService,
        IInscripcionPagoValidationService pagoValidation,
        ILogger<InscripcionesController> logger)
    {
        _service = service;
        _mercadoPagoService = mercadoPagoService;
        _payPalService = payPalService;
        _eventoService = eventoService;
        _tipoAlumnoService = tipoAlumnoService;
        _eventoPrecioService = eventoPrecioService;
        _pagoValidation = pagoValidation;
        _logger = logger;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "anonymous";

    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int? eventoId)
    {
        if (eventoId.HasValue)
            return Ok(await _service.GetByEventoIdAsync(eventoId.Value));
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpGet("export")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Export([FromQuery] int? eventoId, [FromQuery] string? estado)
    {
        var bytes = await _service.ExportToExcelAsync(eventoId, estado);
        var sufijoEvento = eventoId.HasValue ? $"_evento_{eventoId.Value}" : "";
        var sufijoEstado = !string.IsNullOrWhiteSpace(estado) ? $"_{estado.ToLowerInvariant()}" : "";
        var nombre = $"inscripciones{sufijoEvento}{sufijoEstado}_{DateTime.Now:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
    }

    [HttpGet("pendientes")]
    public async Task<IActionResult> GetPendientes([FromQuery] string documento, [FromQuery] int? eventoId)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return BadRequest(new { error = "El documento es requerido." });

        var result = await _service.GetPendientesByDocumentoAsync(documento.Trim(), eventoId);
        return Ok(result);
    }

    [HttpGet("pendientes/count")]
    [Authorize]
    public async Task<IActionResult> CountPendientes()
    {
        var cuit = User.FindFirst("cuit")?.Value;
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        var count = await _service.CountPendientesByDocumentoAsync(cuit.Trim());
        return Ok(new { count });
    }

    [HttpPost("validar-pendientes")]
    [Authorize]
    public async Task<IActionResult> ValidarPendientes()
    {
        var cuit = User.FindFirst("cuit")?.Value;
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        var result = await _pagoValidation.ValidarPendientesPorDocumentoAsync(cuit.Trim());
        return Ok(result);
    }

    [HttpPost("{id}/verificar-mp")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AdminVerificarMP(int id)
    {
        try
        {
            var result = await _pagoValidation.ValidarInscripcionAsync(id);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = $"Inscripcion {id} no encontrada" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InscripcionCreateDto dto)
    {
        _logger.LogInformation(">>> POST /api/inscripciones - EventoId={EventoId}, TipoAlumnoId={TipoAlumnoId}",
            dto.EventoId, dto.TipoAlumnoId);

        var inscripcion = await _service.CrearInscripcionAsync(dto, GetCurrentUser());

        _logger.LogInformation(">>> Inscripcion creada Id={Id}, PrecioFinal={PrecioFinal}",
            inscripcion.Id, inscripcion.PrecioFinal);

        // Categoría extranjera: se cobra en USD vía PayPal (client-side), no por MercadoPago.
        var tipoAlumno = await _tipoAlumnoService.GetByIdAsync(inscripcion.TipoAlumnoId);
        if (tipoAlumno.Extranjero)
        {
            var precios = await _eventoPrecioService.GetByEventoIdAsync(inscripcion.EventoId);
            var precio = precios.FirstOrDefault(p => p.TipoAlumnoId == inscripcion.TipoAlumnoId && p.Activo);
            var montoUsd = precio?.PrecioDolares ?? 0m;
            if (montoUsd <= 0)
                throw new Exceptions.BusinessException(
                    $"La inscripción (#{inscripcion.Id}) se registró, pero la categoría extranjera no tiene un precio en USD configurado.");

            _logger.LogInformation(">>> Inscripcion {Id} es extranjera, pago por PayPal montoUsd={Monto}", inscripcion.Id, montoUsd);
            return Ok(new
            {
                inscripcion.Id,
                inscripcion.PrecioFinal,
                initPoint = (string?)null,
                paypal = true,
                montoUsd,
                moneda = "USD",
            });
        }

        // Si el precio final es 0 (beca 100%, etc.), no necesita pago
        if (inscripcion.PrecioFinal <= 0)
        {
            _logger.LogInformation(">>> PrecioFinal es 0, no se genera link de pago; se confirma directamente");
            await _service.UpdateEstadoAsync(inscripcion.Id, "Confirmada", GetCurrentUser());
            return Ok(new
            {
                inscripcion.Id,
                inscripcion.PrecioFinal,
                initPoint = (string?)null,
                message = "Inscripcion registrada sin costo."
            });
        }

        // Crear preferencia de Mercado Pago
        try
        {
            var evento = await _eventoService.GetByIdAsync(inscripcion.EventoId);
            var cuotas = Math.Clamp(dto.Cuotas, 1, 6);
            decimal montoMP;
            if (dto.ModalidadPago == "reserva" && inscripcion.MontoReserva.HasValue)
                montoMP = inscripcion.MontoReserva.Value;
            else if (cuotas > 1 && inscripcion.PrecioFinalCuotas.HasValue)
                montoMP = inscripcion.PrecioFinalCuotas.Value;
            else
                montoMP = inscripcion.PrecioFinal;
            var mpResult = await _mercadoPagoService.CrearPreferenciaAsync(inscripcion, evento.Titulo, cuotas, montoMP);

            _logger.LogInformation("MP Preference creada: Id={PreferenceId}, InitPoint={InitPoint}",
                mpResult.PreferenceId, mpResult.InitPoint);

            return Ok(new
            {
                inscripcion.Id,
                inscripcion.PrecioFinal,
                mpResult.InitPoint,
                mpResult.PreferenceId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear preferencia de Mercado Pago para inscripcion {InscripcionId}", inscripcion.Id);
            throw new Exceptions.BusinessException($"La inscripcion fue registrada (#{inscripcion.Id}), pero hubo un error al generar el link de pago: {ex.Message}");
        }
    }

    [HttpPost("{id}/generar-pago")]
    public async Task<IActionResult> GenerarPago(int id, [FromBody] GenerarPagoDto? dto)
    {
        var inscripcion = await _service.GetByIdAsync(id);

        if (inscripcion.Estado != "Pendiente" && inscripcion.Estado != "Reservada")
            return BadRequest(new { error = "La inscripcion no esta pendiente de pago." });

        if (inscripcion.PrecioFinal <= 0)
            return BadRequest(new { error = "La inscripcion no requiere pago." });

        try
        {
            var cuotas = Math.Clamp(dto?.Cuotas ?? 1, 1, 6);
            decimal montoMP;
            if (inscripcion.Estado == "Reservada" && inscripcion.MontoReserva.HasValue)
            {
                var resto = inscripcion.PrecioFinal - inscripcion.MontoReserva.Value;
                if (cuotas > 1 && inscripcion.PrecioFinalCuotas.HasValue)
                    montoMP = inscripcion.PrecioFinalCuotas.Value - inscripcion.MontoReserva.Value;
                else
                    montoMP = resto;
            }
            else if (cuotas > 1 && inscripcion.PrecioFinalCuotas.HasValue)
                montoMP = inscripcion.PrecioFinalCuotas.Value;
            else
                montoMP = inscripcion.PrecioFinal;
            var evento = await _eventoService.GetByIdAsync(inscripcion.EventoId);
            var mpResult = await _mercadoPagoService.CrearPreferenciaAsync(inscripcion, evento.Titulo, cuotas, montoMP);

            return Ok(new
            {
                inscripcion.Id,
                inscripcion.PrecioFinal,
                mpResult.InitPoint,
                mpResult.PreferenceId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear preferencia de MP para inscripcion {Id}", id);
            return StatusCode(500, new { error = $"Error al generar el link de pago: {ex.Message}" });
        }
    }

    [HttpGet("{id}/estado-pago")]
    public async Task<IActionResult> GetEstadoPago(int id)
    {
        var inscripcion = await _service.GetByIdAsync(id);

        // Si sigue pendiente o reservada, consultar a MP por si ya se pagó
        if (inscripcion.Estado == "Pendiente" || inscripcion.Estado == "Reservada")
        {
            var paymentInfo = await _mercadoPagoService.BuscarPagoPorReferenciaAsync(
                ExternalReferenceHelper.Build(inscripcion.Id, inscripcion.PublicRef));
            if (paymentInfo != null && paymentInfo.Status == "approved")
            {
                string nuevoEstado;
                if (inscripcion.Estado == "Pendiente" && inscripcion.MontoReserva.HasValue)
                    nuevoEstado = "Reservada";
                else if (inscripcion.Estado == "Reservada")
                    nuevoEstado = "Confirmada";
                else
                    nuevoEstado = "Confirmada";

                if (nuevoEstado != inscripcion.Estado)
                {
                    await _service.UpdateEstadoAsync(id, nuevoEstado, "mercadopago");
                    inscripcion.Estado = nuevoEstado;
                    _logger.LogInformation(">>> Estado actualizado via consulta MP: inscripcion={Id}, estado={Estado}",
                        id, nuevoEstado);
                }
            }
            else if (paymentInfo != null && (paymentInfo.Status == "rejected" || paymentInfo.Status == "cancelled"))
            {
                await _service.UpdateEstadoAsync(id, "Rechazada", "mercadopago");
                inscripcion.Estado = "Rechazada";
            }
        }

        return Ok(new
        {
            inscripcion.Id,
            inscripcion.Estado,
            inscripcion.PrecioFinal,
        });
    }

    /// <summary>
    /// Llamado por el frontend cuando MP redirige de vuelta con payment_id.
    /// Busca en MP todos los pagos de la inscripción, crea/actualiza los rows de Pago
    /// y ajusta el estado de la inscripción. Idempotente — no depende del webhook.
    /// </summary>
    [HttpPost("confirmar-pago")]
    public async Task<IActionResult> ConfirmarPago([FromBody] ConfirmarPagoDto dto)
    {
        _logger.LogInformation(">>> ConfirmarPago: paymentId={PaymentId}, externalReference={ExtRef}",
            dto.PaymentId, dto.ExternalReference);

        // Resolver inscripcionId: preferimos el externalReference enviado por MP en la query;
        // si no vino, consultamos MP por paymentId.
        int inscripcionId;
        if (!ExternalReferenceHelper.TryParseInscripcionId(dto.ExternalReference, out inscripcionId))
        {
            var paymentInfo = await _mercadoPagoService.ObtenerInfoPagoAsync(dto.PaymentId);
            if (paymentInfo == null || !ExternalReferenceHelper.TryParseInscripcionId(paymentInfo.ExternalReference, out inscripcionId))
                return BadRequest(new { error = "Referencia de inscripción inválida." });
        }

        ValidacionInscripcionResult resultado;
        try
        {
            resultado = await _pagoValidation.ValidarInscripcionAsync(inscripcionId);
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = $"Inscripcion {inscripcionId} no encontrada" });
        }

        _logger.LogInformation(
            ">>> ConfirmarPago resultado: inscripcion={Id}, {Anterior}→{Nuevo}, montoAprobado={Monto}, pagosNuevos={Nuevos}",
            inscripcionId, resultado.EstadoAnterior, resultado.EstadoNuevo, resultado.MontoAprobado, resultado.PagosNuevos);

        return Ok(new
        {
            inscripcionId,
            estadoInscripcion = resultado.EstadoNuevo,
            estadoPago = resultado.MontoAprobado > 0 ? "Confirmado" : "Pendiente",
            transactionAmount = resultado.MontoAprobado,
        });
    }

    /// <summary>
    /// Llamado por el frontend tras capturar el pago de PayPal (socios extranjeros).
    /// Registra el Pago en USD y confirma la inscripción. Idempotente por (inscripcion, order).
    /// </summary>
    [HttpPost("confirmar-pago-paypal")]
    public async Task<IActionResult> ConfirmarPagoPayPal([FromBody] ConfirmarPagoPayPalDto dto)
    {
        _logger.LogInformation(">>> ConfirmarPagoPayPal: inscripcion={Id}, orderId={OrderId}, monto={Monto}",
            dto.InscripcionId, dto.OrderId, dto.Monto);

        ConfirmarPagoPayPalResult resultado;
        try
        {
            resultado = await _payPalService.ConfirmarPagoAsync(dto, GetCurrentUser());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        return Ok(new
        {
            inscripcionId = resultado.InscripcionId,
            estadoInscripcion = resultado.EstadoInscripcion,
            estadoPago = resultado.EstadoPago,
            transactionAmount = resultado.Monto,
            currency = resultado.Moneda,
        });
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] InscripcionUpdateEstadoDto dto)
    {
        await _service.UpdateEstadoAsync(id, dto.Estado, GetCurrentUser());
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

public class InscripcionUpdateEstadoDto
{
    public string Estado { get; set; } = string.Empty;
}

public class ConfirmarPagoDto
{
    public long PaymentId { get; set; }
    public string? ExternalReference { get; set; }
}

public class GenerarPagoDto
{
    public int Cuotas { get; set; } = 1;
}
