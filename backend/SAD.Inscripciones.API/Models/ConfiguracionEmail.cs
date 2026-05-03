namespace SAD.Inscripciones.API.Models;

public class ConfiguracionEmail
{
    public int Id { get; set; } = 1;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Usuario { get; set; } = string.Empty;
    public string PasswordCifrado { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Inscripciones SAD";
    public string? ReplyTo { get; set; }
    public string? BccCopia { get; set; }
    public string Asunto { get; set; } = "Confirmación de inscripción - {{Evento}}";
    public bool Activo { get; set; }
    public bool IgnorarCertificadoSsl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
