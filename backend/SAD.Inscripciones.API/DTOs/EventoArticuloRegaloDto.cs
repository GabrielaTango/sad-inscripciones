using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class EventoArticuloRegaloDto
{
    [Required]
    public int EventoId { get; set; }

    [Required]
    public int TipoAlumnoId { get; set; }

    [Required]
    public string ArticuloCodigo { get; set; } = string.Empty;

    public string? DescripcionArticulo { get; set; }

    public int Cantidad { get; set; } = 1;

    public string? CondicionEspecial { get; set; }

    public bool Activo { get; set; } = true;
}
