using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class UsuarioCreateDto
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(4)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    public bool Activo { get; set; } = true;

    [MaxLength(20)]
    public string? CodVended { get; set; }

    public bool EsCapitulo { get; set; }
}
