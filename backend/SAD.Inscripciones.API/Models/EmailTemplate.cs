namespace SAD.Inscripciones.API.Models;

public class EmailTemplate
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyJson { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
