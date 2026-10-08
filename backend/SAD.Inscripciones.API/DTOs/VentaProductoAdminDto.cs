namespace SAD.Inscripciones.API.DTOs;

/// <summary>Fila del listado admin de ventas pagas/pendientes, con DatosExtra ya deserializado.</summary>
public class VentaProductoAdminDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Dictionary<string, string> DatosExtra { get; set; } = new();
    public decimal Importe { get; set; }
    public string Estado { get; set; } = string.Empty;
    public long? MpPaymentId { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaPago { get; set; }
    public bool MailEnviado { get; set; }
    public DateTime UpdatedAt { get; set; }
}
