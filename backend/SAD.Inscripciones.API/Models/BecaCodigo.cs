namespace SAD.Inscripciones.API.Models;

public class BecaCodigo : BaseEntity
{
    public int BecaEventoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public bool Usado { get; set; }
    public DateTime? FechaUso { get; set; }
    public int? InscripcionId { get; set; }
}
