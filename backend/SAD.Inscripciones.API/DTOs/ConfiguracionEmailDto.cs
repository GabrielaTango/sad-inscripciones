namespace SAD.Inscripciones.API.DTOs;

/// <summary>Devuelta al admin. Nunca expone el password.</summary>
public class ConfiguracionEmailDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Usuario { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }
    public string? BccCopia { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public bool IgnorarCertificadoSsl { get; set; }
    public bool TienePassword { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
