namespace SAD.Inscripciones.API.Models;

public class Pago : BaseEntity
{
    public int InscripcionId { get; set; }
    public string MedioPago { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = "Pendiente";
    public decimal Monto { get; set; }
    public string? ReferenciaExterna { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? Observaciones { get; set; }
    public bool SincronizadoTango { get; set; }
}
