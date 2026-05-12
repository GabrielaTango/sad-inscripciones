namespace SAD.Inscripciones.API.DTOs;

/// <summary>Devuelta al admin. Nunca expone el AccessToken.</summary>
public class ConfiguracionMercadoPagoDto
{
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public bool TieneAccessToken { get; set; }
    public string? AccessTokenPreview { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
