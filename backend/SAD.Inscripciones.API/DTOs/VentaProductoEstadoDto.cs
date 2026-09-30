namespace SAD.Inscripciones.API.DTOs;

/// <summary>
/// Estado público de una venta, devuelto al verificar el pago desde la página de
/// resultado. No expone Email, Dni ni DatosExtra: solo lo necesario para mostrar
/// el resultado y, si corresponde, ofrecer reintentar la compra del producto.
/// </summary>
public class VentaProductoEstadoDto
{
    public string Estado { get; set; } = string.Empty;
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Importe { get; set; }
}
