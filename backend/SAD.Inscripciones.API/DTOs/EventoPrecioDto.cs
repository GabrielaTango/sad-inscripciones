using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class EventoPrecioDto
{
    [Required]
    public int EventoId { get; set; }

    [Required]
    public int TipoAlumnoId { get; set; }

    public string? ArticuloCodigo { get; set; }

    [Required]
    public decimal PrecioBase { get; set; }

    public decimal? PrecioCuotas { get; set; }

    public int CantidadCuotas { get; set; } = 6;

    public bool PermiteDescuento { get; set; } = true;

    public bool Activo { get; set; } = true;
}
