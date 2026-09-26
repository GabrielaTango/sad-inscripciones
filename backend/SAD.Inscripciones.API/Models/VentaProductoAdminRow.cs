namespace SAD.Inscripciones.API.Models;

/// <summary>
/// Fila del listado admin de <see cref="VentaProducto"/>: agrega ProductoNombre
/// (JOIN con Productos) y omite PublicRef, que no es de interes administrativo.
/// DatosExtra queda como el JSON crudo; el servicio lo deserializa.
/// </summary>
public class VentaProductoAdminRow
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DatosExtra { get; set; }
    public decimal Importe { get; set; }
    public string Estado { get; set; } = string.Empty;
    public long? MpPaymentId { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime? FechaPago { get; set; }
    public bool MailEnviado { get; set; }
}
