using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

/// <summary>Result of <see cref="IVentaProductoRepository.ConfirmarPagoAsync"/>.</summary>
public enum ConfirmarPagoResult
{
    NotFound,

    /// <summary>
    /// The sale was not (or no longer) Pendiente/Impaga: already confirmed by a
    /// previous call (idempotent replay of the webhook or the result-page
    /// verification). No state change is made.
    /// </summary>
    AlreadyPaid,

    /// <summary>The accredited amount does not match Importe; left as Pendiente for review.</summary>
    AmountMismatch,

    /// <summary>Transitioned Pendiente/Impaga -&gt; Pagada.</summary>
    Confirmed
}

public interface IVentaProductoRepository
{
    Task<int> CreatePendienteAsync(VentaProducto entity);
    Task<VentaProducto?> GetByIdAsync(int id);
    Task<VentaProducto?> GetByPublicRefAsync(string publicRef);

    /// <summary>
    /// Listado admin con JOIN a Productos (ProductoNombre) y filtros opcionales:
    /// producto, estado exacto, rango de COALESCE(FechaPago, FechaAlta) (desde/hasta,
    /// ambos inclusive por dia) y texto libre sobre Dni/Nombre/Apellido/Email.
    /// </summary>
    Task<IEnumerable<VentaProductoAdminRow>> ListAsync(int? productoId, string? estado, DateTime? desde, DateTime? hasta, string? texto);

    /// <summary>
    /// Idempotently confirms payment for a sale under a row lock (SELECT ... FOR
    /// UPDATE): only transitions Pendiente/Impaga -&gt; Pagada, setting MpPaymentId and
    /// FechaPago. Safe to call multiple times with the same webhook payload.
    /// </summary>
    Task<ConfirmarPagoResult> ConfirmarPagoAsync(int ventaId, long mpPaymentId, decimal montoAcreditado);

    Task<IEnumerable<VentaProducto>> ListPendientesAsync();

    /// <summary>
    /// Pendiente -&gt; Impaga solo si la venta tiene mas de <paramref name="horasMinimas"/>
    /// horas desde FechaAlta (comparado en SQL contra el mismo reloj UTC con el que se
    /// escribe). Deja FechaPago en NULL. Devuelve false si no transiciono.
    /// </summary>
    Task<bool> MarcarImpagaAsync(int ventaId, int horasMinimas);

    Task<bool> MarcarMailEnviadoAsync(int ventaId);
}
