namespace SAD.Inscripciones.API.Models;

public class ConfiguracionPayPal
{
    public int Id { get; set; } = 1;
    public string ClientId { get; set; } = string.Empty;
    public string Moneda { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
