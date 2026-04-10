namespace SAD.Inscripciones.API.DTOs;

public class PromocionCuponDisponibleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string TipoDescuento { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public bool Acumulable { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string PromocionNombre { get; set; } = string.Empty;
}
