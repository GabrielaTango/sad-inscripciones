using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class PagoDto
{
    [Required]
    public int InscripcionId { get; set; }

    [Required]
    public string MedioPago { get; set; } = string.Empty;

    [Required]
    public decimal Monto { get; set; }

    public string? ReferenciaExterna { get; set; }

    public DateTime? FechaPago { get; set; }

    public string? Observaciones { get; set; }
}

public class PagoUpdateEstadoDto
{
    [Required]
    public string EstadoPago { get; set; } = string.Empty;
}
