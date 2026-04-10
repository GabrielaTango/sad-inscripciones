using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class TipoAlumnoDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}
