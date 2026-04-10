namespace SAD.Inscripciones.API.Models;

public class EventoProvinciaBeneficio : BaseEntity
{
    public int EventoId { get; set; }
    public string ProvinciaCodigo { get; set; } = string.Empty;
    public bool AplicaPrecioSocio { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public bool Activo { get; set; } = true;
}
