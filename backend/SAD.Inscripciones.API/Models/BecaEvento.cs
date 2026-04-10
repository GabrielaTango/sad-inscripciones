namespace SAD.Inscripciones.API.Models;

public class BecaEvento : BaseEntity
{
    public int EventoId { get; set; }
    public string NombreCampana { get; set; } = string.Empty;
    public string TipoDescuento { get; set; } = string.Empty; // "Porcentaje" o "MontoFijo"
    public decimal Valor { get; set; }
    public int CantidadTotalCodigos { get; set; } = 1;
    public DateTime? FechaVencimiento { get; set; }
    public bool Acumulable { get; set; }
    public bool Activo { get; set; } = true;
}
