namespace SAD.Inscripciones.API.Models;

public class Usuario : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public string? CodVended { get; set; }
    public bool EsCapitulo { get; set; }
}
