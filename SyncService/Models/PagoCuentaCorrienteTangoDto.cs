namespace SyncService.Models;

public class PagoCuentaCorrienteTangoDto
{
    public int Id { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Comprobantes { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public DateTime? FechaPago { get; set; }
    public string? CodVended { get; set; }
    public string? MedioPago { get; set; }
    public int? CtaTesoreria { get; set; }
}

public class ComprobanteImputable
{
    public string TComp { get; set; } = string.Empty;
    public string NComp { get; set; } = string.Empty;
    public DateTime FechaVto { get; set; }
    public decimal Saldo { get; set; }
    public bool EsCuota { get; set; } = true;
}
