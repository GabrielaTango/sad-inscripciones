using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class ContactoDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Asunto { get; set; } = string.Empty;

    [Required]
    public string Mensaje { get; set; } = string.Empty;
}
