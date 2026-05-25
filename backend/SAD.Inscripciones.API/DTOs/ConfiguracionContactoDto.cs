namespace SAD.Inscripciones.API.DTOs;

public class ConfiguracionContactoDto
{
    public string EmailDestino { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class ConfiguracionContactoUpdateDto
{
    public string EmailDestino { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
