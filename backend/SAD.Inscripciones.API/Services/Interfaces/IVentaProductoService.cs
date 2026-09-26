using SAD.Inscripciones.API.DTOs;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IVentaProductoService
{
    /// <summary>
    /// Crea una venta Pendiente para un producto activo, validando los datos del
    /// comprador y sus DatosExtra contra el esquema CamposExtra del producto, y
    /// devuelve el link de pago de MercadoPago.
    /// </summary>
    Task<VentaProductoCreateResultDto> CrearAsync(VentaProductoCreateDto dto);
}
