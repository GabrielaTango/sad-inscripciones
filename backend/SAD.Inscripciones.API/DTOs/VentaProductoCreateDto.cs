using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

/// <summary>Datos del comprador para la compra pública de un Producto.</summary>
public class VentaProductoCreateDto
{
    [Required]
    public int ProductoId { get; set; }

    [Required]
    public string Dni { get; set; } = string.Empty;

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>Respuestas del comprador al esquema CamposExtra del producto.</summary>
    public Dictionary<string, string> DatosExtra { get; set; } = new();
}

/// <summary>Resultado de crear la venta pendiente: id, referencia pública y link de pago de MP.</summary>
public class VentaProductoCreateResultDto
{
    public int VentaId { get; set; }
    public string PublicRef { get; set; } = string.Empty;
    public string InitPoint { get; set; } = string.Empty;
}
