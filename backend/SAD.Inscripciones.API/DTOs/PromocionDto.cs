using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class PromocionDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int? TipoAlumnoId { get; set; }

    [Required]
    public int CantidadCursosRequeridos { get; set; } = 1;

    [Required]
    public int PeriodoMeses { get; set; } = 12;

    [Required]
    public string TipoDescuento { get; set; } = string.Empty;

    [Required]
    public decimal Valor { get; set; }

    public bool Acumulable { get; set; }

    [Required]
    public DateTime FechaVigenciaDesde { get; set; }

    [Required]
    public DateTime FechaVigenciaHasta { get; set; }

    public int DiasValidezCupon { get; set; } = 90;

    public bool Activo { get; set; } = true;
}
