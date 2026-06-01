namespace SAD.Inscripciones.API.DTOs;

/// <summary>
/// Enviado por el frontend tras capturar el pago de PayPal en el navegador.
/// </summary>
public class ConfirmarPagoPayPalDto
{
    public int InscripcionId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Observaciones { get; set; }
}
