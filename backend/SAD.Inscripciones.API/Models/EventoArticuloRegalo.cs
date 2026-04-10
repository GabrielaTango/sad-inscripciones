namespace SAD.Inscripciones.API.Models;

public class EventoArticuloRegalo : BaseEntity
{
    public int EventoId { get; set; }
    public int TipoAlumnoId { get; set; }
    public string ArticuloCodigo { get; set; } = string.Empty;
    public string? DescripcionArticulo { get; set; }
    public int Cantidad { get; set; } = 1;
    public string? CondicionEspecial { get; set; }
    public bool Activo { get; set; } = true;
}
