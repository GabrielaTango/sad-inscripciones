namespace SAD.Inscripciones.API.Models;

public class ConfiguracionMercadoPago
{
    public int Id { get; set; } = 1;
    public string AccessTokenCifrado { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
