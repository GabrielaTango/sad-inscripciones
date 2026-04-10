namespace SAD.Inscripciones.API.Models;

public class PromocionCupon : BaseEntity
{
    public int PromocionId { get; set; }
    public string Documento { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string TipoDescuento { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public bool Acumulable { get; set; }
    public bool Usado { get; set; }
    public DateTime? FechaUso { get; set; }
    public int? InscripcionDestinoId { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}
