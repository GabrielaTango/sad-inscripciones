namespace SAD.Inscripciones.API.Services.Interfaces;

public class ValidacionInscripcionResult
{
    public int InscripcionId { get; set; }
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public decimal MontoAprobado { get; set; }
    public int PagosEncontrados { get; set; }
    public int PagosNuevos { get; set; }
    public bool Cambio => EstadoAnterior != EstadoNuevo;
}

public class ValidacionBatchResult
{
    public int Revisadas { get; set; }
    public int Actualizadas { get; set; }
    public IReadOnlyList<ValidacionInscripcionResult> Detalles { get; set; } = [];
}

public interface IInscripcionPagoValidationService
{
    /// <param name="pagoConocido">
    /// Pago traído por id de MP (webhook / vuelta del checkout). El search de MP es
    /// eventualmente consistente y suele no listar todavía el pago recién hecho, así que
    /// se suma a mano al resultado del search para no perderlo.
    /// </param>
    Task<ValidacionInscripcionResult> ValidarInscripcionAsync(int inscripcionId, MercadoPagoPaymentInfo? pagoConocido = null);
    Task<ValidacionBatchResult> ValidarPendientesPorDocumentoAsync(string documento);
}
