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
    public decimal? MontoReserva { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInscripcion { get; set; }
    public DateTime EventoFechaInicio { get; set; }
    public string EventoModalidad { get; set; } = string.Empty;
}
