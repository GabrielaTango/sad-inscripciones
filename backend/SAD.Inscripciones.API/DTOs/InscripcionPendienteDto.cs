namespace SAD.Inscripciones.API.DTOs;

public class InscripcionPendienteDto
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public string EventoTitulo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public decimal PrecioBase { get; set; }
    public decimal DescuentoAplicado { get; set; }
    public decimal PrecioFinal { get; set; }
    public decimal? PrecioFinalCuotas { get; set; }
    public int? CantidadCuotas { get; set; }
    // Monto ya cobrado como reserva (null si la inscripción todavía no reservó).
    public decimal? MontoReserva { get; set; }
    // Cuánto costaría reservar esta inscripción (30% del precio final). Lo calcula el backend
    // para que el importe del botón coincida exactamente con el que se cobra al reservar.
    public decimal MontoReservaSugerido { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInscripcion { get; set; }
    public DateTime EventoFechaInicio { get; set; }
    public string EventoModalidad { get; set; } = string.Empty;
    // Categoría de extranjero: el cobro se hace en USD por PayPal (PrecioFinal en ARS suele ser 0).
    public bool EsExtranjero { get; set; }
    public decimal? MontoUsd { get; set; }
}
