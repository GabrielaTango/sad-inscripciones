namespace SAD.Inscripciones.API.DTOs;

public class EmailTemplateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyJson { get; set; }
    public bool Activo { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class EmailTemplateUpdateDto
{
    public string Asunto { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyJson { get; set; }
    public bool Activo { get; set; } = true;
}

public class EmailTemplateListItemDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime UpdatedAt { get; set; }
}
