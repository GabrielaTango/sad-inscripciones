namespace SyncService.Models;

public class DebitoAutomaticoSyncDto
{
    public string Cuit { get; set; } = string.Empty;
    public string EstadoSync { get; set; } = string.Empty;
    public string? MarcaTarjeta { get; set; }
    public string? NumeroTarjeta { get; set; }
    public string? Vencimiento { get; set; }
}
