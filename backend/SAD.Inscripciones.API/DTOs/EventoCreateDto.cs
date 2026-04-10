using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class EventoCreateDto
{
    [Required]
    public int TipoEventoId { get; set; }

    [Required]
    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    public DateTime FechaInicio { get; set; }

    [Required]
    public DateTime FechaFin { get; set; }

    [Required]
    public DateTime FechaCierreInscripcion { get; set; }

    public string? Lugar { get; set; }

    [Required]
    public string Modalidad { get; set; } = string.Empty;

    public int? MaxInscriptos { get; set; }
}
