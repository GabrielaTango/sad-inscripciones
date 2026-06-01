namespace SAD.Inscripciones.API.DTOs;

/// <summary>Devuelta al admin. El Client-ID es público, así que se expone completo.</summary>
public class ConfiguracionPayPalDto
{
    public string ClientId { get; set; } = string.Empty;
    public string Moneda { get; set; } = "USD";
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
