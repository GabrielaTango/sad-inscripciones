namespace SAD.Inscripciones.API.DTOs;

/// <summary>
/// Update payload. Si Password es null o vacío, se preserva el valor actual.
/// Si trae texto, se cifra y reemplaza.
/// </summary>
public class ConfiguracionEmailUpdateDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Usuario { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyTo { get; set; }
    public string? BccCopia { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public bool IgnorarCertificadoSsl { get; set; }
}
