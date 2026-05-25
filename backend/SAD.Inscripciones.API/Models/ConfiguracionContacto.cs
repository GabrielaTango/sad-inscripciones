namespace SAD.Inscripciones.API.Models;

public class ConfiguracionContacto
{
    public int Id { get; set; } = 1;
    public string EmailDestino { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
