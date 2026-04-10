namespace SAD.Inscripciones.API.Models;

public class Evento : BaseEntity
{
    public int TipoEventoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public DateTime FechaCierreInscripcion { get; set; }
    public string? Lugar { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public int? MaxInscriptos { get; set; }
    public bool Activo { get; set; } = true;
    public bool SoloSocios { get; set; }
    public string? TerminosArchivo { get; set; }
}
