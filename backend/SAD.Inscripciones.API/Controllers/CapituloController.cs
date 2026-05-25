using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/capitulo")]
[Authorize(Policy = "Capitulo")]
public class CapituloController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly IDebitoAutomaticoService _debitoService;
    private readonly ILogger<CapituloController> _logger;

    private static readonly HashSet<string> MediosPagoValidos = new(StringComparer.OrdinalIgnoreCase) { "Contado", "Transferencia" };

    public CapituloController(DbConnectionFactory dbFactory, IDebitoAutomaticoService debitoService, ILogger<CapituloController> logger)
    {
        _dbFactory = dbFactory;
        _debitoService = debitoService;
        _logger = logger;
    }

    private string? GetCodVended() => User.FindFirst("codVended")?.Value;

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var codVended = GetCodVended();
        if (string.IsNullOrEmpty(codVended)) return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var v = await conn.QueryFirstOrDefaultAsync<VendedorDto>(
            "SELECT CodVended, CtaCaja, CtaTransferencia, CtaCuotas, CtaOtra FROM Vendedores WHERE CodVended = @CodVended",
            new { CodVended = codVended });

        if (v == null)
            return NotFound(new { error = "Vendedor no sincronizado desde Tango" });

        return Ok(v);
    }

    [HttpGet("socios")]
    public async Task<IActionResult> BuscarSocios([FromQuery] string? q)
    {
        var codVended = GetCodVended();
        if (string.IsNullOrEmpty(codVended)) return Unauthorized();

        var term = (q ?? string.Empty).Trim();

        using var conn = _dbFactory.CreateConnection();
        var sql = @"
            SELECT c.Cuit, c.RazonSoci, c.Domicilio, c.CodPostal, c.CodProvin,
                   COALESCE(SUM(rc.Saldo), 0) AS Saldo
            FROM Clientes c
            LEFT JOIN ResumenCuenta rc ON rc.Cuit = c.Cuit
            WHERE c.CodVended = @CodVended" +
            (string.IsNullOrEmpty(term) ? string.Empty : " AND (c.Cuit LIKE @Like OR c.RazonSoci LIKE @Like)") +
            @" GROUP BY c.Cuit, c.RazonSoci, c.Domicilio, c.CodPostal, c.CodProvin
              ORDER BY c.RazonSoci
              LIMIT 50";

        var socios = await conn.QueryAsync<SocioCapituloDto>(sql,
            new { CodVended = codVended, Like = $"%{term}%" });

        return Ok(socios);
    }

    [HttpGet("socios/{cuit}/resumen")]
    public async Task<IActionResult> GetResumen(string cuit)
    {
        var codVended = GetCodVended();
        if (string.IsNullOrEmpty(codVended)) return Unauthorized();

        using var conn = _dbFactory.CreateConnection();

        var pertenece = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Clientes WHERE Cuit = @Cuit AND CodVended = @CodVended",
            new { Cuit = cuit, CodVended = codVended });
        if (pertenece == 0) return Forbid();

        var locked = await GetLockedComprobantesAsync(conn, cuit);

        var movimientos = await conn.QueryAsync<ResumenItemDto>(
            "SELECT Id, Cuit, TComp, NComp, FechaVto, Saldo FROM ResumenCuenta WHERE Cuit = @Cuit ORDER BY FechaVto ASC",
            new { Cuit = cuit });

        // No se filtran los bloqueados: se marcan con PendienteSync para que el capítulo
        // vea que ya hay un cobro generado y aún no se reflejó en Tango.
        foreach (var m in movimientos)
        {
            m.PendienteSync = locked.Contains(LockKey(m.TComp, m.NComp, m.FechaVto));
        }

        return Ok(movimientos);
    }

    private static string LockKey(string? tComp, string? nComp, DateTime fechaVto)
        => $"{tComp?.Trim()}|{nComp?.Trim()}|{fechaVto:yyyy-MM-dd}";

    [HttpPost("cobros")]
    public async Task<IActionResult> RegistrarCobro([FromBody] RegistrarCobroRequest request)
    {
        var codVended = GetCodVended();
        if (string.IsNullOrEmpty(codVended)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Cuit))
            return BadRequest(new { error = "Cuit requerido" });

        if (string.IsNullOrWhiteSpace(request.MedioPago) || !MediosPagoValidos.Contains(request.MedioPago))
            return BadRequest(new { error = "Medio de pago inválido. Permitidos: Contado, Transferencia" });

        if (request.Monto <= 0)
            return BadRequest(new { error = "El monto debe ser mayor a cero" });

        using var conn = _dbFactory.CreateConnection();

        var vendedor = await conn.QueryFirstOrDefaultAsync<VendedorDto>(
            "SELECT CodVended, CtaCaja, CtaTransferencia, CtaCuotas, CtaOtra FROM Vendedores WHERE CodVended = @CodVended",
            new { CodVended = codVended });
        if (vendedor == null)
            return BadRequest(new { error = "Vendedor no sincronizado desde Tango" });

        // Cuenta haber: depende del medio de pago.
        //   - Contado        → CtaCaja
        //   - Transferencia  → CtaTransferencia
        var esContado = string.Equals(request.MedioPago, "Contado", StringComparison.OrdinalIgnoreCase);
        var ctaTesoreria = esContado ? vendedor.CtaCaja : vendedor.CtaTransferencia;
        if (ctaTesoreria <= 0)
        {
            var nombreCta = esContado ? "CTA_CAJA" : "CTA_TRANSFERENCIA";
            return BadRequest(new { error = $"El vendedor no tiene configurada {nombreCta} en Tango (CAMPOS_ADICIONALES de GVA23)" });
        }

        var pertenece = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Clientes WHERE Cuit = @Cuit AND CodVended = @CodVended",
            new { Cuit = request.Cuit, CodVended = codVended });
        if (pertenece == 0) return Forbid();

        string? comprobantesJson = null;
        if (request.Ids != null && request.Ids.Length > 0)
        {
            var movimientos = (await conn.QueryAsync<ResumenItemDto>(
                "SELECT Id, Cuit, TComp, NComp, FechaVto, Saldo FROM ResumenCuenta WHERE Cuit = @Cuit AND Id IN @Ids ORDER BY FechaVto ASC",
                new { Cuit = request.Cuit, Ids = request.Ids })).ToList();

            if (movimientos.Count != request.Ids.Length)
                return BadRequest(new { error = "Algún comprobante seleccionado no pertenece al socio" });

            var locked = await GetLockedComprobantesAsync(conn, request.Cuit);
            if (movimientos.Any(m => locked.Contains(LockKey(m.TComp, m.NComp, m.FechaVto))))
                return BadRequest(new { error = "Algún comprobante/vencimiento ya tiene un pago en proceso" });

            var totalSaldos = movimientos.Sum(m => m.Saldo);
            if (Math.Abs(totalSaldos - request.Monto) > 0.01m)
                return BadRequest(new { error = $"El monto ({request.Monto}) no coincide con la suma de los comprobantes ({totalSaldos})" });

            comprobantesJson = JsonSerializer.Serialize(movimientos.Select(m => new { m.TComp, m.NComp, m.FechaVto, m.Saldo }));
        }

        var externalReference = $"CAP-{codVended}-{request.Cuit}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var id = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO PagosCuentaCorriente
                (Cuit, Monto, Comprobantes, ExternalReference, EstadoPago, FechaPago,
                 CodVended, MedioPago, CtaTesoreria, CreatedAt, UpdatedAt)
            VALUES
                (@Cuit, @Monto, @Comprobantes, @ExternalReference, 'Aprobado', UTC_TIMESTAMP(),
                 @CodVended, @MedioPago, @CtaTesoreria, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();",
            new
            {
                request.Cuit,
                request.Monto,
                Comprobantes = comprobantesJson,
                ExternalReference = externalReference,
                CodVended = codVended,
                request.MedioPago,
                CtaTesoreria = ctaTesoreria,
            });

        _logger.LogInformation(
            "Cobro capítulo registrado: id={Id}, codVended={CV}, cuit={Cuit}, monto={Monto}, medio={Medio}, comprobantes={Cnt}",
            id, codVended, request.Cuit, request.Monto, request.MedioPago, request.Ids?.Length ?? 0);

        return Ok(new { id, externalReference, monto = request.Monto });
    }

    [HttpGet("socios/{cuit}/debito-automatico")]
    public async Task<IActionResult> GetDebitoAutomatico(string cuit)
    {
        var codVended = GetCodVended();
        if (string.IsNullOrEmpty(codVended)) return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var pertenece = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Clientes WHERE Cuit = @Cuit AND CodVended = @CodVended",
            new { Cuit = cuit, CodVended = codVended });
        if (pertenece == 0) return Forbid();

        var info = await _debitoService.GetByCuitAsync(cuit);
        return Ok(info ?? new DebitoAutomaticoInfo());
    }

    [HttpPost("socios/{cuit}/debito-automatico")]
    public async Task<IActionResult> GuardarDebitoAutomatico(string cuit, [FromBody] GuardarDebitoAutomaticoRequest request)
    {
        var codVended = GetCodVended();
        if (string.IsNullOrEmpty(codVended)) return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var pertenece = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Clientes WHERE Cuit = @Cuit AND CodVended = @CodVended",
            new { Cuit = cuit, CodVended = codVended });
        if (pertenece == 0) return Forbid();

        try
        {
            await _debitoService.GuardarAsync(cuit, request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        _logger.LogInformation("Débito automático actualizado por capítulo {CV} para CUIT={Cuit}", codVended, cuit);
        return Ok(await _debitoService.GetByCuitAsync(cuit));
    }

    [HttpDelete("socios/{cuit}/debito-automatico")]
    public async Task<IActionResult> DarDeBajaDebitoAutomatico(string cuit)
    {
        var codVended = GetCodVended();
        if (string.IsNullOrEmpty(codVended)) return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var pertenece = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Clientes WHERE Cuit = @Cuit AND CodVended = @CodVended",
            new { Cuit = cuit, CodVended = codVended });
        if (pertenece == 0) return Forbid();

        try
        {
            await _debitoService.DarDeBajaAsync(cuit);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        _logger.LogInformation("Baja débito automático por capítulo {CV} para CUIT={Cuit}", codVended, cuit);
        return Ok(await _debitoService.GetByCuitAsync(cuit));
    }

    [HttpGet("cobros")]
    public async Task<IActionResult> ListarCobros()
    {
        var codVended = GetCodVended();
        if (string.IsNullOrEmpty(codVended)) return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var cobros = await conn.QueryAsync<CobroCapituloDto>(@"
            SELECT Id, Cuit, Monto, Comprobantes, MedioPago, ExternalReference,
                   EstadoPago, SincronizadoTango, FechaPago, CreatedAt
            FROM PagosCuentaCorriente
            WHERE CodVended = @CodVended
            ORDER BY CreatedAt DESC
            LIMIT 200",
            new { CodVended = codVended });

        return Ok(cobros);
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private async Task<HashSet<string>> GetLockedComprobantesAsync(System.Data.IDbConnection conn, string cuit)
    {
        var lockedJson = await conn.QueryAsync<string>(@"
            SELECT Comprobantes FROM PagosCuentaCorriente
            WHERE Cuit = @Cuit AND EstadoPago IN ('Pendiente', 'Aprobado') AND SincronizadoTango = 0",
            new { Cuit = cuit });

        var locked = new HashSet<string>();
        foreach (var json in lockedJson)
        {
            if (string.IsNullOrEmpty(json)) continue;
            try
            {
                var items = JsonSerializer.Deserialize<List<ComprobanteRefLock>>(json, JsonOpts);
                if (items == null) continue;
                foreach (var item in items)
                    locked.Add(LockKey(item.TComp, item.NComp, item.FechaVto));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON inválido en PagosCuentaCorriente.Comprobantes para CUIT {Cuit}", cuit);
            }
        }
        return locked;
    }

    private sealed class ComprobanteRefLock
    {
        public string? TComp { get; set; }
        public string? NComp { get; set; }
        public DateTime FechaVto { get; set; }
    }
}

public class VendedorDto
{
    public string CodVended { get; set; } = string.Empty;
    public int CtaCaja { get; set; }
    public int CtaTransferencia { get; set; }
    public int CtaCuotas { get; set; }
    public int CtaOtra { get; set; }
}

public class SocioCapituloDto
{
    public string Cuit { get; set; } = string.Empty;
    public string RazonSoci { get; set; } = string.Empty;
    public string? Domicilio { get; set; }
    public string? CodPostal { get; set; }
    public string? CodProvin { get; set; }
    public decimal Saldo { get; set; }
}

public class ResumenItemDto
{
    public int Id { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public string TComp { get; set; } = string.Empty;
    public string NComp { get; set; } = string.Empty;
    public DateTime FechaVto { get; set; }
    public decimal Saldo { get; set; }
    public bool PendienteSync { get; set; }
}

public class RegistrarCobroRequest
{
    public string Cuit { get; set; } = string.Empty;
    public string MedioPago { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public int[]? Ids { get; set; }
}

public class CobroCapituloDto
{
    public int Id { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Comprobantes { get; set; }
    public string? MedioPago { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public bool SincronizadoTango { get; set; }
    public DateTime? FechaPago { get; set; }
    public DateTime CreatedAt { get; set; }
}
