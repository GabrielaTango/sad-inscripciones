using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.DTOs;

/// <summary>Full product response, for the admin (includes mail subject/body and inactive products).</summary>
public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
    public string? ImagenUrl { get; set; }
    public List<CampoExtraProducto> CamposExtra { get; set; } = new();
    public string? MailAsunto { get; set; }
    public string? MailCuerpoHtml { get; set; }
    public DateTime FechaAlta { get; set; }
}

/// <summary>
/// Public product response for anonymous GETs. Never exposes MailAsunto/MailCuerpoHtml.
/// </summary>
public class ProductoPublicoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string? ImagenUrl { get; set; }
    public List<CampoExtraProducto> CamposExtra { get; set; } = new();
}
