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

    /// <summary>
    /// UpdatedAt of the stored image (ProductoImagenes), null when the product has
    /// none. Read-only projection filled by the LEFT JOIN; the blob is never loaded here.
    /// </summary>
    public DateTime? ImagenUpdatedAt { get; set; }

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
