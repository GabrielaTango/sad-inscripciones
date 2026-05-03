namespace SyncService.Models;

public class DatosClienteTango
{
    public string? Documento { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Domicilio { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }
    public string? Celular { get; set; }

    public static DatosClienteTango FromInscripcion(InscripcionTangoDto i) => new()
    {
        Documento = i.Documento,
        Nombre = i.Nombre,
        Apellido = i.Apellido,
        Email = i.Email,
        Telefono = i.Telefono,
        Domicilio = i.Domicilio,
        CodigoPostal = i.CodigoPostal,
        Localidad = i.Localidad,
        Provincia = i.Provincia,
        Celular = i.Celular,
    };

    public static DatosClienteTango FromPago(PagoTangoDto p) => new()
    {
        Documento = p.Documento,
        Nombre = p.Nombre,
        Apellido = p.Apellido,
        Email = p.Email,
        Telefono = p.Telefono,
        Domicilio = p.Domicilio,
        CodigoPostal = p.CodigoPostal,
        Localidad = p.Localidad,
        Provincia = p.Provincia,
        Celular = p.Celular,
    };
}
