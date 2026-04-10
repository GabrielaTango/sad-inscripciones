namespace SAD.Inscripciones.API.Models;

public class TipoAlumno : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
