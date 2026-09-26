using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

/// <summary>Result of <see cref="IVentaProductoRepository.ConfirmarPagoAsync"/>.</summary>
public enum ConfirmarPagoResult
{
    NotFound,

    /// <summary>
    /// The sale was not (or no longer) Pendiente: already confirmed by a
    /// previous call (idempotent replay of the webhook or the result-page
    /// verification). No state change is made.
    /// </summary>
    AlreadyPaid,

    /// <summary>The accredited amount does not match Importe; left as Pendiente for review.</summary>
    AmountMismatch,

    /// <summary>Transitioned Pendiente -&gt; Pagada.</summary>
    Confirmed
}

public interface IVentaProductoRepository
{
    Task<int> CreatePendienteAsync(VentaProducto entity);
    Task<VentaProducto?> GetByIdAsync(int id);
    Task<VentaProducto?> GetByPublicRefAsync(string publicRef);
    Task<IEnumerable<VentaProducto>> ListAsync(int? productoId, string? estado);

    /// <summary>
    /// Idempotently confirms payment for a sale under a row lock (SELECT ... FOR
    /// UPDATE): only transitions Pendiente -&gt; Pagada, setting MpPaymentId and
    /// FechaPago. Safe to call multiple times with the same webhook payload.
    /// </summary>
    Task<ConfirmarPagoResult> ConfirmarPagoAsync(int ventaId, long mpPaymentId, decimal montoAcreditado);

    Task<bool> MarcarMailEnviadoAsync(int ventaId);
}
