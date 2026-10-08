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

    /// <summary>
    /// Punto compartido por <c>VentaProductoWebhookController</c> y la verificación
    /// de la página de resultado (<see cref="VerificarAsync"/>): parsea el
    /// external_reference con <see cref="VentaExternalReference"/> (ignora si no es
    /// una referencia de venta), valida que el PublicRef coincida con el de la venta
    /// cargada y, solo si <c>paymentInfo.Status == "approved"</c>, confirma el pago
    /// vía <c>ConfirmarPagoAsync</c>. Cualquier otro estado se loguea y la venta
    /// queda Pendiente. Idempotente: seguro de llamar varias veces con el mismo pago.
    /// </summary>
    Task ProcesarPagoAsync(MercadoPagoPaymentInfo paymentInfo);

    /// <summary>
    /// Usado por la página pública de resultado: si la venta no esta Pagada, busca
    /// los pagos de MercadoPago por external_reference y confirma los aprobados vía
    /// <see cref="ProcesarPagoAsync"/>; luego devuelve el estado actual de la venta.
    /// </summary>
    Task<VentaProductoEstadoDto> VerificarAsync(string publicRef);

    /// <summary>
    /// Consulta en MercadoPago todas las ventas Pendiente: confirma las que tengan un
    /// pago aprobado y marca Impaga las de mas de 24 h sin ningun pago que pueda
    /// aprobarse todavia. Una venta que falla no aborta al resto.
    /// </summary>
    Task<VentaProductoConsultaResultadoDto> ConsultarPendientesAsync();

    /// <summary>
    /// Listado admin de ventas con filtros (producto, estado, rango de FechaPago,
    /// texto libre); ProductoNombre viene del JOIN del repositorio y DatosExtra se
    /// deserializa desde el JSON crudo.
    /// </summary>
    Task<IEnumerable<VentaProductoAdminDto>> ListAdminAsync(int? productoId, string? estado, DateTime? desde, DateTime? hasta, string? texto);

    /// <summary>
    /// Exporta el mismo listado a Excel. Cuando hay filtro de producto, una columna
    /// por cada CamposExtra del producto (en su orden); si no, la union de claves
    /// presentes en los resultados.
    /// </summary>
    Task<byte[]> ExportToExcelAsync(int? productoId, string? estado, DateTime? desde, DateTime? hasta, string? texto);
}
