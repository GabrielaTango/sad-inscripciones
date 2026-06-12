namespace SAD.Inscripciones.API.Models;

public class Inscripcion : BaseEntity
{
    public int EventoId { get; set; }
    public int TipoAlumnoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Documento { get; set; }
    public string? Provincia { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal DescuentoAplicado { get; set; }
    public decimal PrecioFinal { get; set; }
    public decimal? PrecioFinalCuotas { get; set; }
    public int? CantidadCuotas { get; set; }
    public decimal? MontoReserva { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? Observaciones { get; set; }
    public DateTime FechaInscripcion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaNacimiento { get; set; }
    public string? Domicilio { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Localidad { get; set; }
    public string? Pais { get; set; }
    public string? Celular { get; set; }
    public string? Profesion { get; set; }
    public string? Especialidad { get; set; }
    public string? Institucion { get; set; }
    public string? Sector { get; set; }
    public bool SincronizadoTango { get; set; }

    // No persistido: se completan solo en los listados (join a TiposAlumno / Pagos).
    public bool Extranjero { get; set; }
    public decimal? MontoDolares { get; set; }
    // Token único usado en el external_reference de MercadoPago (ver ExternalReferenceHelper).
    public string? PublicRef { get; set; }
}
