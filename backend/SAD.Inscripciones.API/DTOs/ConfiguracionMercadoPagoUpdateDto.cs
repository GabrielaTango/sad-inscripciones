namespace SAD.Inscripciones.API.DTOs;

/// <summary>
/// Update payload. Si AccessToken es null o vacío, se preserva el valor actual.
/// Si trae texto, se cifra y reemplaza.
/// </summary>
public class ConfiguracionMercadoPagoUpdateDto
{
    public string? AccessToken { get; set; }
    public string FrontendBaseUrl { get; set; } = string.Empty;
}
