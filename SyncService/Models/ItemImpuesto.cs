namespace SyncService.Models;

public class ItemImpuesto
{
    public string CodArticu { get; set; } = string.Empty;
    public decimal PORC_IVA { get; set; }
    public decimal PORC_IB { get; set; }
    public decimal PORC_II { get; set; }
    public bool INCLUY_IVA { get; set; }
    public bool INCLUY_IMP { get; set; }
}
