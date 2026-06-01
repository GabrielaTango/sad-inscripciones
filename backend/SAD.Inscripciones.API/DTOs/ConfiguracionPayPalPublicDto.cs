namespace SAD.Inscripciones.API.DTOs;

/// <summary>
/// Config pública que consume el frontend para cargar el SDK de PayPal en el navegador.
/// El Client-ID no es secreto.
/// </summary>
public class ConfiguracionPayPalPublicDto
{
    public string ClientId { get; set; } = string.Empty;
    public string Moneda { get; set; } = "USD";
}
