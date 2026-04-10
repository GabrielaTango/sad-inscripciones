using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class CambiarPasswordDto
{
    [Required]
    public string PasswordActual { get; set; } = string.Empty;

    [Required]
    [MinLength(4)]
    public string PasswordNueva { get; set; } = string.Empty;
}
