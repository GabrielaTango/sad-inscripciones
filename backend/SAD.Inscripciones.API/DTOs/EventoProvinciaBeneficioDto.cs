using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class EventoProvinciaBeneficioDto
{
    [Required]
    public int EventoId { get; set; }

    [Required]
    public string ProvinciaCodigo { get; set; } = string.Empty;

    public bool AplicaPrecioSocio { get; set; }

    [Required]
    public decimal PorcentajeDescuento { get; set; }

    public bool Activo { get; set; } = true;
}
