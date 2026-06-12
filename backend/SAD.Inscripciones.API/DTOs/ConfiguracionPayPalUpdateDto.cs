namespace SAD.Inscripciones.API.DTOs;

/// <summary>Update payload del admin para la configuración de PayPal.</summary>
public class ConfiguracionPayPalUpdateDto
{
    public string ClientId { get; set; } = string.Empty;
    public string Moneda { get; set; } = "USD";
}
