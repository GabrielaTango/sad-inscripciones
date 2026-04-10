namespace SAD.Inscripciones.API.Models;

public class TipoEvento : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
