namespace SyncService.Models;

public class InscripcionTangoDto
{
    public int Id { get; set; }
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
    public decimal PrecioFinal { get; set; }
    public string Moneda { get; set; } = "ARS";
    public DateTime FechaInscripcion { get; set; }
    public int EventoId { get; set; }
    public string? EventoTitulo { get; set; }
    public string? CodArticu { get; set; }
}
