namespace SAD.Inscripciones.API.Services.Interfaces;

public class DebitoAutomaticoInfo
{
    public bool Activo { get; set; }
    public string? MarcaTarjeta { get; set; }
    public string? TarjetaUltimos4 { get; set; }
    public string? Vencimiento { get; set; }
    public DateTime? FechaAlta { get; set; }
    public string EstadoSync { get; set; } = "Sincronizado";
    public bool Bloqueado => EstadoSync != "Sincronizado";
}

public class GuardarDebitoAutomaticoRequest
{
    public string MarcaTarjeta { get; set; } = string.Empty;
    public string NumeroTarjeta { get; set; } = string.Empty;
    public string Vencimiento { get; set; } = string.Empty;
}

public interface IDebitoAutomaticoService
{
    Task<DebitoAutomaticoInfo?> GetByCuitAsync(string cuit);
    Task GuardarAsync(string cuit, GuardarDebitoAutomaticoRequest request);
    Task DarDeBajaAsync(string cuit);
}
