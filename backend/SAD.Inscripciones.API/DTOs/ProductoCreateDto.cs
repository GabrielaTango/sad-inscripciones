using System.ComponentModel.DataAnnotations;
using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.DTOs;

public class ProductoCreateDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Required]
    public decimal Precio { get; set; }

    public bool Activo { get; set; } = true;

    public string? ImagenUrl { get; set; }

    public List<CampoExtraProducto> CamposExtra { get; set; } = new();

    public string? MailAsunto { get; set; }
    public string? MailCuerpoHtml { get; set; }
}
