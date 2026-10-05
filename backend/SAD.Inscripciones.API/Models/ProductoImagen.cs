namespace SAD.Inscripciones.API.Models;

/// <summary>Binary image of a product, stored in its own table so product queries never load the blob.</summary>
public class ProductoImagen
{
    public int ProductoId { get; set; }
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
