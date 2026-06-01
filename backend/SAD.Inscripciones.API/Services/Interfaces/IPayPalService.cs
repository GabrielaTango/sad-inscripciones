using SAD.Inscripciones.API.DTOs;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IPayPalService
{
    /// <summary>
    /// Registra el pago capturado en PayPal (USD) y confirma la inscripción.
    /// Idempotente por (InscripcionId, OrderId).
    /// </summary>
    Task<ConfirmarPagoPayPalResult> ConfirmarPagoAsync(ConfirmarPagoPayPalDto dto, string actor);
}

public class ConfirmarPagoPayPalResult
{
    public int InscripcionId { get; set; }
    public string EstadoInscripcion { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
}
