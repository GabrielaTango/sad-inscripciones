using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Data;

namespace SAD.Inscripciones.API.Controllers;

public record ArticuloDto(string CodArticu, string Descripcion);

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class ArticulosController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;

    public ArticulosController(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string? q)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            SELECT CodArticu, Descripcio AS Descripcion
            FROM Articulos
            WHERE (@q IS NULL OR CodArticu LIKE CONCAT('%', @q, '%') OR Descripcio LIKE CONCAT('%', @q, '%'))
            ORDER BY Descripcio
            LIMIT 50";
        var result = await connection.QueryAsync<ArticuloDto>(sql, new { q });
        return Ok(result);
    }
}
