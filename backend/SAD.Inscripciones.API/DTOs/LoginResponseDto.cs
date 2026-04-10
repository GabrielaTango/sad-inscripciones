namespace SAD.Inscripciones.API.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
