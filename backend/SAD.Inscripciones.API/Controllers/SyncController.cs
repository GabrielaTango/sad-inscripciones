using Dapper;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Data;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly string _apiKey;

    public SyncController(DbConnectionFactory dbFactory, IConfiguration configuration)
    {
        _dbFactory = dbFactory;
        _apiKey = configuration["SyncSettings:ApiKey"] ?? "";
    }

    private bool ValidateApiKey()
    {
        if (string.IsNullOrEmpty(_apiKey)) return false;
        var key = Request.Headers["X-Sync-Key"].FirstOrDefault();
        return key == _apiKey;
    }

    // --- CLIENTES ---

    [HttpPost("clientes")]
    public async Task<IActionResult> UpsertCliente([FromBody] SyncClienteDto dto)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Clientes (Cuit, RazonSoci, Domicilio, CodPostal, CodProvin)
            VALUES (@Cuit, @RazonSoci, @Domicilio, @CodPostal, @CodProvin)
            ON DUPLICATE KEY UPDATE RazonSoci=@RazonSoci, Domicilio=@Domicilio, CodPostal=@CodPostal, CodProvin=@CodProvin", dto);
        return Ok();
    }

    [HttpDelete("clientes/{cuit}")]
    public async Task<IActionResult> DeleteCliente(string cuit)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Clientes WHERE Cuit = @Cuit", new { Cuit = cuit });
        return Ok();
    }

    // --- ARTICULOS ---

    [HttpPost("articulos")]
    public async Task<IActionResult> UpsertArticulo([FromBody] SyncArticuloDto dto)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Articulos (CodArticu, Descripcio)
            VALUES (@CodArticu, @Descripcio)
            ON DUPLICATE KEY UPDATE Descripcio=@Descripcio", dto);
        return Ok();
    }

    [HttpDelete("articulos/{codArticu}")]
    public async Task<IActionResult> DeleteArticulo(string codArticu)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Articulos WHERE CodArticu = @CodArticu", new { CodArticu = codArticu });
        return Ok();
    }

    // --- PROVINCIAS ---

    [HttpPost("provincias")]
    public async Task<IActionResult> UpsertProvincia([FromBody] SyncProvinciaDto dto)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Provincias (Codigo, Nombre)
            VALUES (@Codigo, @Nombre)
            ON DUPLICATE KEY UPDATE Nombre=@Nombre", dto);
        return Ok();
    }

    [HttpDelete("provincias/{codigo}")]
    public async Task<IActionResult> DeleteProvincia(string codigo)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Provincias WHERE Codigo = @Codigo", new { Codigo = codigo });
        return Ok();
    }
    // --- INSCRIPCIONES (para sincronizar a Tango) ---

    [HttpGet("inscripciones")]
    public async Task<IActionResult> GetInscripcionesPendientesTango()
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        var inscripciones = await conn.QueryAsync<SyncInscripcionDto>(@"
            SELECT i.Id, i.Documento, i.Nombre, i.Apellido, i.Email, i.Telefono,
                   i.Domicilio, i.CodigoPostal, i.Localidad, i.Provincia, i.Celular,
                   i.PrecioFinal, i.FechaInscripcion, i.EventoId,
                   e.Titulo AS EventoTitulo,
                   (SELECT ep.ArticuloCodigo FROM EventoPrecios ep
                    WHERE ep.EventoId = i.EventoId AND ep.TipoAlumnoId = i.TipoAlumnoId AND ep.Activo = 1 LIMIT 1) AS CodArticu
            FROM Inscripciones i
            INNER JOIN Eventos e ON e.Id = i.EventoId
            WHERE i.Estado = 'Confirmada'
              AND i.SincronizadoTango = 0
              AND i.DeletedAt IS NULL");
        return Ok(inscripciones);
    }

    [HttpPatch("inscripciones/{id}/tango")]
    public async Task<IActionResult> MarcarSincronizadoTango(int id)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Inscripciones SET SincronizadoTango = 1 WHERE Id = @Id",
            new { Id = id });
        return Ok();
    }

    // --- CUENTA CORRIENTE ---

    [HttpPost("cuenta-corriente")]
    public async Task<IActionResult> SyncCuentaCorriente([FromBody] SyncCuentaCorrienteDto dto)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync("DELETE FROM ResumenCuenta WHERE Cuit = @Cuit", new { dto.Cuit }, tx);

        if (dto.Movimientos?.Length > 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO ResumenCuenta (Cuit, TComp, NComp, FechaVto, Saldo)
                VALUES (@Cuit, @TComp, @NComp, @FechaVto, @Saldo)",
                dto.Movimientos.Select(m => new { dto.Cuit, m.TComp, m.NComp, m.FechaVto, m.Saldo }), tx);
        }

        tx.Commit();
        return Ok();
    }
}

public class SyncClienteDto
{
    public string Cuit { get; set; } = string.Empty;
    public string RazonSoci { get; set; } = string.Empty;
    public string? Domicilio { get; set; }
    public string? CodPostal { get; set; }
    public string? CodProvin { get; set; }
}

public class SyncArticuloDto
{
    public string CodArticu { get; set; } = string.Empty;
    public string Descripcio { get; set; } = string.Empty;
}

public class SyncProvinciaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class SyncCuentaCorrienteDto
{
    public string Cuit { get; set; } = string.Empty;
    public SyncMovimientoDto[] Movimientos { get; set; } = [];
}

public class SyncMovimientoDto
{
    public string TComp { get; set; } = string.Empty;
    public string NComp { get; set; } = string.Empty;
    public DateTime FechaVto { get; set; }
    public decimal Saldo { get; set; }
}

public class SyncInscripcionDto
{
    public int Id { get; set; }
    public string? Documento { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Domicilio { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }
    public string? Celular { get; set; }
    public decimal PrecioFinal { get; set; }
    public DateTime FechaInscripcion { get; set; }
    public int EventoId { get; set; }
    public string? EventoTitulo { get; set; }
    public string? CodArticu { get; set; }
}
