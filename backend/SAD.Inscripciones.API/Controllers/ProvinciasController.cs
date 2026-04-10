using Dapper;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Data;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvinciasController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;

    public ProvinciasController(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var connection = _dbFactory.CreateConnection();
        var provincias = await connection.QueryAsync<ProvinciaDto>(
            "SELECT Codigo AS CodProvin, Nombre AS NombrePro FROM Provincias ORDER BY Nombre");
        return Ok(provincias);
    }
}

public class ProvinciaDto
{
    public string CodProvin { get; set; } = string.Empty;
    public string NombrePro { get; set; } = string.Empty;
}
