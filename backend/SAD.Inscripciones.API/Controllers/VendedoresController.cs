using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Data;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class VendedoresController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;

    public VendedoresController(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _dbFactory.CreateConnection();
        var vendedores = await conn.QueryAsync<VendedorListItemDto>(
            "SELECT CodVended, CtaCaja, CtaTransferencia, CtaCuotas, CtaOtra FROM Vendedores ORDER BY CodVended");
        return Ok(vendedores);
    }
}

public class VendedorListItemDto
{
    public string CodVended { get; set; } = string.Empty;
    public int CtaCaja { get; set; }
    public int CtaTransferencia { get; set; }
    public int CtaCuotas { get; set; }
    public int CtaOtra { get; set; }
}
