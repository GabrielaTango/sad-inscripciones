namespace SAD.Inscripciones.API.Models;

/// <summary>
/// One purchase attempt of a Producto. Only counts as a sale once
/// <see cref="Estado"/> reaches "Pagada", set idempotently by the MercadoPago
/// webhook via the UNIQUE <see cref="MpPaymentId"/>.
/// </summary>
public class VentaProducto : BaseEntity
{
    public int ProductoId { get; set; }

    /// <summary>Public identifier used in the MercadoPago external reference and result page.</summary>
    public string PublicRef { get; set; } = string.Empty;

    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// JSON object with the buyer's answers to the product's CamposExtra schema.
    /// Kept as a raw string on the DB model; services deserialize it as needed.
    /// </summary>
    public string? DatosExtra { get; set; }

    public decimal Importe { get; set; }

    /// <summary>Pendiente, Pagada or Rechazada.</summary>
    public string Estado { get; set; } = "Pendiente";

    public long? MpPaymentId { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime? FechaPago { get; set; }
    public bool MailEnviado { get; set; }
}
