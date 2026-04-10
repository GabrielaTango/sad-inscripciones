using System.ComponentModel.DataAnnotations;

namespace SAD.Inscripciones.API.DTOs;

public class InscripcionCreateDto
{
    [Required]
    public int EventoId { get; set; }

    [Required]
    public int TipoAlumnoId { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Apellido { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    [Required]
    public string Documento { get; set; } = string.Empty;

    public string? Provincia { get; set; }

    public string? CodigoBeca { get; set; }

    public string? CodigoCupon { get; set; }

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

    public int Cuotas { get; set; } = 1;

    public string? ModalidadPago { get; set; }
}
