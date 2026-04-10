namespace SAD.Inscripciones.API.DTOs;

public class SocioDataDto
{
    public string Documento { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Domicilio { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Provincia { get; set; }
}
