using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/webhooks/mercadopago")]
public class MercadoPagoWebhookController : ControllerBase
{
    private readonly IMercadoPagoService _mpService;
    private readonly IPagoRepository _pagoRepository;
    private readonly IInscripcionRepository _inscripcionRepository;
    private readonly IInscripcionService _inscripcionService;
    private readonly ILogger<MercadoPagoWebhookController> _logger;

    public MercadoPagoWebhookController(
        IMercadoPagoService mpService,
        IPagoRepository pagoRepository,
        IInscripcionRepository inscripcionRepository,
        IInscripcionService inscripcionService,
        ILogger<MercadoPagoWebhookController> logger)
    {
        _mpService = mpService;
        _pagoRepository = pagoRepository;
        _inscripcionRepository = inscripcionRepository;
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

        if (!int.TryParse(paymentInfo.ExternalReference, out var inscripcionId))
        {
            _logger.LogWarning("ExternalReference invalida: {Ref}", paymentInfo.ExternalReference);
            return Ok();
        }

        var inscripcion = await _inscripcionRepository.GetByIdAsync(inscripcionId);
        if (inscripcion == null)
        {
            _logger.LogWarning("Inscripcion {Id} no encontrada para pago MP", inscripcionId);
            return Ok();
        }

        // Mapear estado de MP a nuestro estado
        var estadoPago = paymentInfo.Status switch
        {
            "approved" => "Confirmado",
            "pending" or "in_process" or "authorized" => "Pendiente",
            "rejected" or "cancelled" or "refunded" or "charged_back" => "Rechazado",
            _ => "Pendiente"
        };

        // Crear o actualizar registro de pago
        var pagosExistentes = await _pagoRepository.GetByInscripcionIdAsync(inscripcionId);
        var pagoExistente = pagosExistentes.FirstOrDefault(p =>
            p.ReferenciaExterna == paymentId.ToString() && p.DeletedAt == null);

        if (pagoExistente != null)
        {
            pagoExistente.EstadoPago = estadoPago;
            pagoExistente.UpdatedBy = "mercadopago";
            if (estadoPago == "Confirmado")
                pagoExistente.FechaPago = DateTime.UtcNow;
            await _pagoRepository.UpdateAsync(pagoExistente);
        }
        else
        {
            var pago = new Pago
            {
                InscripcionId = inscripcionId,
                MedioPago = $"MercadoPago ({paymentInfo.PaymentMethodId})",
                EstadoPago = estadoPago,
                Monto = paymentInfo.TransactionAmount,
                ReferenciaExterna = paymentId.ToString(),
                FechaPago = estadoPago == "Confirmado" ? DateTime.UtcNow : null,
                Observaciones = $"MP Payment ID: {paymentId} - Status: {paymentInfo.Status}/{paymentInfo.StatusDetail}",
                CreatedBy = "mercadopago",
                UpdatedBy = "mercadopago"
            };
            await _pagoRepository.CreateAsync(pago);
        }

        // Actualizar estado de inscripcion
        var estadoInscripcion = estadoPago switch
        {
            "Confirmado" => "Confirmada",
            "Rechazado" => "Rechazada",
            _ => inscripcion.Estado // No cambiar si sigue pendiente
        };

        if (estadoInscripcion != inscripcion.Estado)
        {
            // Pasamos por el service para que dispare side-effects (cupones, mail).
            await _inscripcionService.UpdateEstadoAsync(inscripcionId, estadoInscripcion, "mercadopago");
        }

        _logger.LogInformation("Pago MP {PaymentId} procesado: estado={Estado} inscripcion={InscripcionId}",
            paymentId, estadoPago, inscripcionId);

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
