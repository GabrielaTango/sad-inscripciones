using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class BecaEventoDto
{
    [Required]
    public int EventoId { get; set; }

    [Required]
    public string NombreCampana { get; set; } = string.Empty;

    [Required]
    public string TipoDescuento { get; set; } = string.Empty;

    [Required]
    public decimal Valor { get; set; }

    [Required]
    public int CantidadTotalCodigos { get; set; } = 1;

    public DateTime? FechaVencimiento { get; set; }

    public bool Acumulable { get; set; }

    public bool Activo { get; set; } = true;
}
