using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class ResetPasswordDto
{
    [Required]
    [MinLength(4)]
    public string PasswordNueva { get; set; } = string.Empty;
}
