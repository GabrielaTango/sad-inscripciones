namespace SAD.Inscripciones.API.Models;

/// <summary>
/// A sellable product (e.g. a t-shirt), managed from the admin and sold through
/// the standalone product-sale module. Never synced to Tango.
/// </summary>
public class Producto : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;
    public string? ImagenUrl { get; set; }

    /// <summary>
    /// JSON array of <see cref="CampoExtraProducto"/> describing the dynamic
    /// extra fields shown on the public purchase form. Kept as a raw string on
    /// the DB model; services deserialize it as needed.
    /// </summary>
    public string? CamposExtra { get; set; }

    public string? MailAsunto { get; set; }
    public string? MailCuerpoHtml { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
}
