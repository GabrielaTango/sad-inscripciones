using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public class MercadoPagoPreferenceResult
{
    public string PreferenceId { get; set; } = string.Empty;
    public string InitPoint { get; set; } = string.Empty;
}

public interface IMercadoPagoService
{
    Task<MercadoPagoPreferenceResult> CrearPreferenciaAsync(Inscripcion inscripcion, string eventoTitulo, int cuotas = 1, decimal? montoOverride = null);
    Task<MercadoPagoPaymentInfo?> ObtenerInfoPagoAsync(long paymentId);
    Task<MercadoPagoPaymentInfo?> BuscarPagoPorReferenciaAsync(string externalReference);
    Task<IReadOnlyList<MercadoPagoPaymentInfo>> BuscarTodosPagosPorReferenciaAsync(string externalReference);

    /// <summary>
    /// Carga la config actual de MercadoPago desde la DB (con cache), aplica el AccessToken
    /// al SDK global y devuelve el FrontendBaseUrl configurado.
    /// </summary>
    Task<string> EnsureConfiguradoAsync();

    /// <summary>Invalida el cache de configuración (después de un PUT al panel admin).</summary>
    void InvalidarCache();
}

public class MercadoPagoPaymentInfo
{
    public long Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDetail { get; set; } = string.Empty;
    public decimal TransactionAmount { get; set; }
    public string? ExternalReference { get; set; }
    public string? PaymentMethodId { get; set; }
}
