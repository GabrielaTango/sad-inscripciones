using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
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

    // Construye un INSERT multi-row con upsert en una sola query
    // (MySQL 8.0.20+: INSERT ... VALUES (...) AS new ON DUPLICATE KEY UPDATE col = new.col).
    // Reemplaza el patrón Dapper.ExecuteAsync(sql, items) que iteraba 1 query por item.
    private static (string sql, DynamicParameters parameters) BuildBulkUpsert<T>(
        string table,
        string[] columns,
        string[] updateColumns,
        IReadOnlyList<T> items,
        Func<T, object?[]> getValues)
    {
        var parameters = new DynamicParameters();
        var values = new StringBuilder();

        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0) values.Append(", ");
            values.Append('(');
            var rowValues = getValues(items[i]);
            for (var c = 0; c < columns.Length; c++)
            {
                if (c > 0) values.Append(", ");
                var pname = $"p{i}_{columns[c]}";
                values.Append('@').Append(pname);
                parameters.Add(pname, rowValues[c]);
            }
            values.Append(')');
        }

        var colList = string.Join(", ", columns);
        var updates = string.Join(", ", updateColumns.Select(c => $"{c} = new.{c}"));
        var sql = $"INSERT INTO {table} ({colList}) VALUES {values} AS new ON DUPLICATE KEY UPDATE {updates}";
        return (sql, parameters);
    }

    // --- CLIENTES ---

    [HttpPost("clientes")]
    public async Task<IActionResult> UpsertCliente([FromBody] SyncClienteDto dto)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(SqlClientes, dto);
        return Ok();
    }

    [HttpPost("clientes-batch")]
    public async Task<IActionResult> UpsertClientesBatch([FromBody] SyncClienteDto[] items)
    {
        if (!ValidateApiKey()) return Unauthorized();
        if (items == null || items.Length == 0) return Ok(new { count = 0 });

        var (sql, parameters) = BuildBulkUpsert(
            "Clientes",
            new[] { "Cuit", "RazonSoci", "Domicilio", "CodPostal", "CodProvin", "CodVended", "CodClient" },
            new[] { "RazonSoci", "Domicilio", "CodPostal", "CodProvin", "CodVended", "CodClient" },
            items,
            c => new object?[] { c.Cuit, c.RazonSoci, c.Domicilio, c.CodPostal, c.CodProvin, c.CodVended, c.CodClient });

        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(sql, parameters);
        return Ok(new { count = items.Length });
    }

    private const string SqlClientes = @"
        INSERT INTO Clientes (Cuit, RazonSoci, Domicilio, CodPostal, CodProvin, CodVended, CodClient)
        VALUES (@Cuit, @RazonSoci, @Domicilio, @CodPostal, @CodProvin, @CodVended, @CodClient)
        ON DUPLICATE KEY UPDATE RazonSoci=@RazonSoci, Domicilio=@Domicilio, CodPostal=@CodPostal, CodProvin=@CodProvin, CodVended=@CodVended, CodClient=@CodClient";

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
        await conn.ExecuteAsync(SqlArticulos, dto);
        return Ok();
    }

    [HttpPost("articulos-batch")]
    public async Task<IActionResult> UpsertArticulosBatch([FromBody] SyncArticuloDto[] items)
    {
        if (!ValidateApiKey()) return Unauthorized();
        if (items == null || items.Length == 0) return Ok(new { count = 0 });

        var (sql, parameters) = BuildBulkUpsert(
            "Articulos",
            new[] { "CodArticu", "Descripcio" },
            new[] { "Descripcio" },
            items,
            a => new object?[] { a.CodArticu, a.Descripcio });

        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(sql, parameters);
        return Ok(new { count = items.Length });
    }

    private const string SqlArticulos = @"
        INSERT INTO Articulos (CodArticu, Descripcio)
        VALUES (@CodArticu, @Descripcio)
        ON DUPLICATE KEY UPDATE Descripcio=@Descripcio";

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
        await conn.ExecuteAsync(SqlProvincias, dto);
        return Ok();
    }

    [HttpPost("provincias-batch")]
    public async Task<IActionResult> UpsertProvinciasBatch([FromBody] SyncProvinciaDto[] items)
    {
        if (!ValidateApiKey()) return Unauthorized();
        if (items == null || items.Length == 0) return Ok(new { count = 0 });

        var (sql, parameters) = BuildBulkUpsert(
            "Provincias",
            new[] { "Codigo", "Nombre" },
            new[] { "Nombre" },
            items,
            p => new object?[] { p.Codigo, p.Nombre });

        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(sql, parameters);
        return Ok(new { count = items.Length });
    }

    private const string SqlProvincias = @"
        INSERT INTO Provincias (Codigo, Nombre)
        VALUES (@Codigo, @Nombre)
        ON DUPLICATE KEY UPDATE Nombre=@Nombre";

    [HttpDelete("provincias/{codigo}")]
    public async Task<IActionResult> DeleteProvincia(string codigo)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Provincias WHERE Codigo = @Codigo", new { Codigo = codigo });
        return Ok();
    }
    // --- VENDEDORES ---

    [HttpPost("vendedores")]
    public async Task<IActionResult> UpsertVendedor([FromBody] SyncVendedorDto dto)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(SqlVendedores, dto);
        return Ok();
    }

    [HttpPost("vendedores-batch")]
    public async Task<IActionResult> UpsertVendedoresBatch([FromBody] SyncVendedorDto[] items)
    {
        if (!ValidateApiKey()) return Unauthorized();
        if (items == null || items.Length == 0) return Ok(new { count = 0 });

        var (sql, parameters) = BuildBulkUpsert(
            "Vendedores",
            new[] { "CodVended", "CtaCaja", "CtaTransferencia", "CtaCuotas", "CtaOtra" },
            new[] { "CtaCaja", "CtaTransferencia", "CtaCuotas", "CtaOtra" },
            items,
            v => new object?[] { v.CodVended, v.CtaCaja, v.CtaTransferencia, v.CtaCuotas, v.CtaOtra });

        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(sql, parameters);
        return Ok(new { count = items.Length });
    }

    private const string SqlVendedores = @"
        INSERT INTO Vendedores (CodVended, CtaCaja, CtaTransferencia, CtaCuotas, CtaOtra)
        VALUES (@CodVended, @CtaCaja, @CtaTransferencia, @CtaCuotas, @CtaOtra)
        ON DUPLICATE KEY UPDATE
            CtaCaja=@CtaCaja,
            CtaTransferencia=@CtaTransferencia,
            CtaCuotas=@CtaCuotas,
            CtaOtra=@CtaOtra";

    [HttpDelete("vendedores/{codVended}")]
    public async Task<IActionResult> DeleteVendedor(string codVended)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Vendedores WHERE CodVended = @CodVended", new { CodVended = codVended });
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
                   COALESCE((
                       SELECT SUM(p.Monto)
                       FROM Pagos p
                       WHERE p.InscripcionId = i.Id
                         AND p.EstadoPago = 'Confirmado'
                         AND p.DeletedAt IS NULL
                   ), i.PrecioFinal) AS PrecioFinal,
                   i.FechaInscripcion, i.EventoId,
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

    // --- PAGOS (para sincronizar recibos a Tango) ---

    [HttpGet("pagos")]
    public async Task<IActionResult> GetPagosPendientesTango()
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        var pagos = await conn.QueryAsync<SyncPagoDto>(@"
            SELECT p.Id, p.InscripcionId, p.Monto, p.MedioPago, p.ReferenciaExterna, p.FechaPago,
                   i.Documento, i.Nombre, i.Apellido, i.Email, i.Telefono,
                   i.Domicilio, i.CodigoPostal, i.Localidad, i.Provincia, i.Celular
            FROM Pagos p
            INNER JOIN Inscripciones i ON i.Id = p.InscripcionId
            WHERE p.EstadoPago = 'Confirmado'
              AND p.SincronizadoTango = 0
              AND p.DeletedAt IS NULL
              AND i.DeletedAt IS NULL
            ORDER BY p.Id");
        return Ok(pagos);
    }

    [HttpPatch("pagos/{id}/tango")]
    public async Task<IActionResult> MarcarPagoSincronizadoTango(int id)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Pagos SET SincronizadoTango = 1 WHERE Id = @Id",
            new { Id = id });
        return Ok();
    }

    // --- PAGOS CUENTA CORRIENTE (recibos de resumen de cuenta para sincronizar a Tango) ---

    [HttpGet("pagos-cuenta-corriente")]
    public async Task<IActionResult> GetPagosCuentaCorrientePendientesTango()
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        var pagos = await conn.QueryAsync<SyncPagoCuentaCorrienteDto>(@"
            SELECT Id, Cuit, Monto, Comprobantes, ExternalReference, FechaPago,
                   CodVended, MedioPago, CtaTesoreria
            FROM PagosCuentaCorriente
            WHERE EstadoPago = 'Aprobado'
              AND SincronizadoTango = 0
            ORDER BY Id");
        return Ok(pagos);
    }

    [HttpPatch("pagos-cuenta-corriente/{id}/tango")]
    public async Task<IActionResult> MarcarPagoCuentaCorrienteSincronizado(int id, [FromBody] MarcarPagoCCSyncDto dto)
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE PagosCuentaCorriente
            SET SincronizadoTango = 1, MontoImputado = @MontoImputado, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id",
            new { Id = id, MontoImputado = dto.MontoImputado });
        return Ok();
    }

    // --- TRIGGER MANUAL DE FULL SYNC ---

    [HttpPost("trigger")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> TriggerFullSync()
    {
        var usuario = User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "admin";
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE SyncTrigger
            SET RequestedAt = UTC_TIMESTAMP(), RequestedBy = @RequestedBy
            WHERE Id = 1",
            new { RequestedBy = usuario });
        return Ok(new { requestedAt = DateTime.UtcNow });
    }

    [HttpGet("trigger/status")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetTriggerStatus()
    {
        using var conn = _dbFactory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<SyncTriggerStatusDto>(
            "SELECT RequestedAt, RequestedBy, ConsumedAt FROM SyncTrigger WHERE Id = 1");
        return Ok(row ?? new SyncTriggerStatusDto());
    }

    [HttpGet("trigger")]
    public async Task<IActionResult> ConsumeTrigger()
    {
        if (!ValidateApiKey()) return Unauthorized();
        using var conn = _dbFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var pending = await conn.QueryFirstOrDefaultAsync<DateTime?>(
            "SELECT RequestedAt FROM SyncTrigger WHERE Id = 1 AND RequestedAt IS NOT NULL FOR UPDATE",
            transaction: tx);

        if (pending is null)
        {
            tx.Commit();
            return Ok(new { pending = false });
        }

        await conn.ExecuteAsync(@"
            UPDATE SyncTrigger
            SET ConsumedAt = UTC_TIMESTAMP(), RequestedAt = NULL
            WHERE Id = 1",
            transaction: tx);
        tx.Commit();
        return Ok(new { pending = true, requestedAt = pending });
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
    public string? CodVended { get; set; }
    public string? CodClient { get; set; }
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

public class SyncVendedorDto
{
    public string CodVended { get; set; } = string.Empty;
    public int CtaCaja { get; set; }
    public int CtaTransferencia { get; set; }
    public int CtaCuotas { get; set; }
    public int CtaOtra { get; set; }
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

public class SyncPagoDto
{
    public int Id { get; set; }
    public int InscripcionId { get; set; }
    public decimal Monto { get; set; }
    public string MedioPago { get; set; } = string.Empty;
    public string? ReferenciaExterna { get; set; }
    public DateTime? FechaPago { get; set; }
    // Datos del socio (desde Inscripciones) para ensure cliente en Tango
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
}

public class SyncPagoCuentaCorrienteDto
{
    public int Id { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Comprobantes { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public DateTime? FechaPago { get; set; }
    public string? CodVended { get; set; }
    public string? MedioPago { get; set; }
    public int? CtaTesoreria { get; set; }
}

public class MarcarPagoCCSyncDto
{
    public decimal MontoImputado { get; set; }
}

public class SyncTriggerStatusDto
{
    public DateTime? RequestedAt { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
