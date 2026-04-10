namespace SAD.Inscripciones.API.Models;

public class EventoPrecio : BaseEntity
{
    public int EventoId { get; set; }
    public int TipoAlumnoId { get; set; }
    public string? ArticuloCodigo { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal? PrecioCuotas { get; set; }
    public int CantidadCuotas { get; set; } = 6;
    public bool PermiteDescuento { get; set; } = true;
    public bool Activo { get; set; } = true;
}
