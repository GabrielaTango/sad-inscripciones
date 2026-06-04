using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly IUsuarioService _usuarioService;

    public AuthController(DbConnectionFactory dbFactory, IConfiguration configuration, IUsuarioService usuarioService)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
        _usuarioService = usuarioService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        // 1. Try internal users table first (BCrypt)
        var usuario = await _usuarioService.ValidateCredentialsAsync(loginDto.Usuario, loginDto.Password);
        if (usuario != null)
        {
            var esCapitulo = usuario.EsCapitulo && !string.IsNullOrEmpty(usuario.CodVended);
            var role = esCapitulo ? "Capitulo" : "Admin";
            var token = GenerateJwtToken(usuario.Username, role: role, codVended: esCapitulo ? usuario.CodVended : null);
            return Ok(new LoginResponseDto
            {
                Token = token,
                Cuit = usuario.Username,
                IsAdmin = !esCapitulo,
                EsCapitulo = esCapitulo,
                CodVended = esCapitulo ? usuario.CodVended : null,
            });
        }

        // 2. Fallback to GVA14 (CUIT-based auth)
        if (loginDto.Usuario != loginDto.Password)
            return Unauthorized(new { message = "Credenciales invalidas." });

        var cuit = loginDto.Usuario;

        using var connection = _dbFactory.CreateConnection();
        // Pueden ingresar al portal los socios reales (COD_CLIENT '0…') y los clientes 'P…'.
        // OJO: los 'P…' pueden loguearse pero NO son socios — el endpoint socio-data sigue
        // filtrando solo '0…', así que el flag esSocio del frontend queda en false y, en la
        // inscripción, InscripcionService rechaza las categorías de socio para estos clientes.
        // El resto ('L…' y otros) NO puede ingresar.
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Cuit FROM Clientes WHERE Cuit = @Cuit AND (CodClient LIKE '0%' OR CodClient LIKE 'P%')",
            new { Cuit = cuit });

        if (result == null)
            return Unauthorized(new { message = "Credenciales invalidas." });

        var gvaToken = GenerateJwtToken(cuit);

        return Ok(new LoginResponseDto
        {
            Token = gvaToken,
            Cuit = cuit
        });
    }

    [Authorize]
    [HttpGet("socio-data")]
    public async Task<IActionResult> GetSocioData()
    {
        var cuit = User.FindFirst("cuit")?.Value;
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        var socio = await BuscarSocioEnClientes(cuit);
        if (socio == null)
            return NotFound(new { message = "No se encontraron datos del socio." });

        return Ok(socio);
    }

    [HttpGet("socio-data/{cuit}")]
    public async Task<IActionResult> GetSocioDataByCuit(string cuit)
    {
        if (string.IsNullOrWhiteSpace(cuit))
            return BadRequest(new { message = "CUIT requerido." });

        var socio = await BuscarSocioEnClientes(cuit);
        if (socio == null)
            return NotFound(new { message = "No se encontraron datos del socio." });

        return Ok(socio);
    }

    private async Task<SocioDataDto?> BuscarSocioEnClientes(string cuit)
    {
        using var connection = _dbFactory.CreateConnection();
        // Solo socios reales (COD_CLIENT '0…'). Esto define el flag esSocio del frontend
        // y, por ende, el autocompletado de datos y el bucket de precios de socio.
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT Cuit, RazonSoci, Domicilio, CodPostal, CodProvin FROM Clientes WHERE Cuit = @Cuit AND CodClient LIKE '0%'",
            new { Cuit = cuit });

        if (result == null)
            return null;

        string razonSoci = result.RazonSoci?.ToString()?.Trim() ?? "";
        var apellido = razonSoci;
        var nombre = "";

        var commaIndex = razonSoci.IndexOf(',');
        if (commaIndex >= 0)
        {
            apellido = razonSoci[..commaIndex].Trim();
            nombre = razonSoci[(commaIndex + 1)..].Trim();
        }

        return new SocioDataDto
        {
            Documento = result.Cuit?.ToString()?.Trim() ?? "",
            Apellido = apellido,
            Nombre = nombre,
            Domicilio = result.Domicilio?.ToString()?.Trim(),
            CodigoPostal = result.CodPostal?.ToString()?.Trim(),
            Provincia = result.CodProvin?.ToString()?.Trim(),
        };
    }

    private string GenerateJwtToken(string identity, string? role = null, string? codVended = null)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, identity),
            new Claim("cuit", identity)
        };

        if (!string.IsNullOrEmpty(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (!string.IsNullOrEmpty(codVended))
            claims.Add(new Claim("codVended", codVended));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
