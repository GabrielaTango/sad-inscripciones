using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

/// <summary>
/// Webhook dedicado para pagos de ventas de producto, separado del webhook de
/// inscripciones (<see cref="MercadoPagoWebhookController"/>): comparte el mismo
/// parseo de notificación de MP pero delega toda la lógica de confirmación a
/// <see cref="IVentaProductoService.ProcesarPagoAsync"/>.
/// </summary>
[ApiController]
[Route("api/webhooks/mercadopago/ventas")]
public class VentaProductoWebhookController : ControllerBase
{
    private readonly IMercadoPagoService _mpService;
    private readonly IVentaProductoService _ventaService;
    private readonly ILogger<VentaProductoWebhookController> _logger;

    public VentaProductoWebhookController(
        IMercadoPagoService mpService,
        IVentaProductoService ventaService,
        ILogger<VentaProductoWebhookController> logger)
    {
        _mpService = mpService;
        _ventaService = ventaService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Notification([FromBody] MercadoPagoNotification notification)
    {
        _logger.LogInformation("MP Webhook (ventas) recibido: type={Type}, action={Action}, dataId={DataId}",
            notification.Type, notification.Action, notification.Data?.Id);

        // Solo procesamos notificaciones de pago.
        if (notification.Type != "payment" || notification.Data?.Id == null)
            return Ok();

        if (!long.TryParse(notification.Data.Id, out var paymentId))
            return Ok();

        try
        {
            var paymentInfo = await _mpService.ObtenerInfoPagoAsync(paymentId);
            if (paymentInfo == null)
            {
                _logger.LogWarning("No se pudo obtener info del pago (venta) {PaymentId}", paymentId);
                return Ok();
            }

            await _ventaService.ProcesarPagoAsync(paymentInfo);
            return Ok();
        }
        catch (Exception ex)
        {
            // Excepción inesperada (no un caso de negocio ya manejado en ProcesarPagoAsync):
            // devolvemos 500 para que MP reintente la notificación más tarde.
            _logger.LogError(ex, "Error inesperado procesando webhook de venta para pago {PaymentId}", paymentId);
            return StatusCode(500);
        }
    }
}
