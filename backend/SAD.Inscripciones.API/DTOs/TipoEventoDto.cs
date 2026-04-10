using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class TipoEventoDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}
