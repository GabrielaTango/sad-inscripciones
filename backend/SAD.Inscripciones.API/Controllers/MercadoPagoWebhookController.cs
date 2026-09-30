using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Services;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/webhooks/mercadopago")]
public class MercadoPagoWebhookController : ControllerBase
{
    private readonly IMercadoPagoService _mpService;
    private readonly IInscripcionPagoValidationService _pagoValidation;
    private readonly IInscripcionService _inscripcionService;
    private readonly ILogger<MercadoPagoWebhookController> _logger;

    public MercadoPagoWebhookController(
        IMercadoPagoService mpService,
        IInscripcionPagoValidationService pagoValidation,
        IInscripcionService inscripcionService,
        ILogger<MercadoPagoWebhookController> logger)
    {
        _mpService = mpService;
        _pagoValidation = pagoValidation;
        _inscripcionService = inscripcionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Notification([FromBody] MercadoPagoNotification notification)
    {
        _logger.LogInformation("MP Webhook recibido: type={Type}, action={Action}, dataId={DataId}",
            notification.Type, notification.Action, notification.Data?.Id);

        // Solo procesamos notificaciones de pago
        if (notification.Type != "payment" || notification.Data?.Id == null)
            return Ok();

        if (!long.TryParse(notification.Data.Id, out var paymentId))
            return Ok();

        var paymentInfo = await _mpService.ObtenerInfoPagoAsync(paymentId);
        if (paymentInfo == null)
        {
            _logger.LogWarning("No se pudo obtener info del pago {PaymentId}", paymentId);
            return Ok();
        }

        if (!ExternalReferenceHelper.TryParseInscripcionId(paymentInfo.ExternalReference, out var inscripcionId))
        {
            _logger.LogWarning("ExternalReference invalida: {Ref}", paymentInfo.ExternalReference);
            return Ok();
        }

        // Registra/actualiza los pagos de la inscripcion y resuelve el estado comparando lo
        // acreditado contra PrecioFinal y MontoReserva: pagar la reserva (30%) deja la
        // inscripcion en "Reservada", no en "Confirmada". Es la misma logica que usa
        // confirmar-pago, asi que webhook y vuelta del checkout no se pisan entre si.
        ValidacionInscripcionResult resultado;
        try
        {
            resultado = await _pagoValidation.ValidarInscripcionAsync(inscripcionId, paymentInfo);
        }
        catch (ArgumentException)
        {
            _logger.LogWarning("Inscripcion {Id} no encontrada para pago MP", inscripcionId);
            return Ok();
        }

        // Rechazo: solo aplica si no hay nada acreditado. Un rechazo posterior (reintento de
        // tarjeta, contracargo de una cuota) no debe pisar una inscripcion ya reservada o pagada.
        var esRechazo = paymentInfo.Status is "rejected" or "cancelled" or "refunded" or "charged_back";
        if (esRechazo && resultado.MontoAprobado <= 0 && resultado.EstadoNuevo == "Pendiente")
        {
            await _inscripcionService.UpdateEstadoAsync(inscripcionId, "Rechazada", "mercadopago");
            _logger.LogInformation("Pago MP {PaymentId} rechazado ({Status}): inscripcion {Id} → Rechazada",
                paymentId, paymentInfo.Status, inscripcionId);
            return Ok();
        }

        _logger.LogInformation(
            "Pago MP {PaymentId} procesado: inscripcion={Id}, {Anterior}→{Nuevo}, montoAprobado={Monto}",
            paymentId, inscripcionId, resultado.EstadoAnterior, resultado.EstadoNuevo, resultado.MontoAprobado);

        return Ok();
    }
}

public class MercadoPagoNotification
{
    public string? Action { get; set; }
    public string? Type { get; set; }
    public MercadoPagoNotificationData? Data { get; set; }
}

public class MercadoPagoNotificationData
{
    public string? Id { get; set; }
}
