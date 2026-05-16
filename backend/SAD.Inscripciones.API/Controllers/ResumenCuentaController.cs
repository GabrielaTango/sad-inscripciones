using System.Security.Claims;
using System.Text.Json;
using Dapper;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Helpers;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/resumen-cuenta")]
[Authorize]
public class ResumenCuentaController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly IMercadoPagoService _mpService;
    private readonly ILogger<ResumenCuentaController> _logger;

    public ResumenCuentaController(DbConnectionFactory dbFactory, IMercadoPagoService mpService, ILogger<ResumenCuentaController> logger)
    {
        _dbFactory = dbFactory;
        _mpService = mpService;
        _logger = logger;
    }

    private string? GetCuit() => User.FindFirst("cuit")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value;

    [HttpGet]
    public async Task<IActionResult> GetByCuit()
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        using var conn = _dbFactory.CreateConnection();

        var locked = await GetLockedComprobantesAsync(conn, cuit);

        var movimientos = await conn.QueryAsync<ResumenCuentaDto>(
            "SELECT Id, Cuit, TComp, NComp, FechaVto, Saldo FROM ResumenCuenta WHERE Cuit = @Cuit ORDER BY FechaVto ASC",
            new { Cuit = cuit });

        var filtrados = locked.Count == 0
            ? movimientos
            : movimientos.Where(m => !locked.Contains(LockKey(m.TComp, m.NComp, m.FechaVto)));

        return Ok(filtrados);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        using var conn = _dbFactory.CreateConnection();

        var locked = await GetLockedComprobantesAsync(conn, cuit);

        var movimientos = await conn.QueryAsync<ResumenCuentaDto>(
            "SELECT TComp, NComp, FechaVto FROM ResumenCuenta WHERE Cuit = @Cuit",
            new { Cuit = cuit });

        var count = locked.Count == 0
            ? movimientos.Count()
            : movimientos.Count(m => !locked.Contains(LockKey(m.TComp, m.NComp, m.FechaVto)));

        return Ok(new { count });
    }

    [HttpGet("meta")]
    public async Task<IActionResult> GetMeta()
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<(string? CodClient, string? RazonSoci)>(
            "SELECT CodClient, RazonSoci FROM Clientes WHERE Cuit = @Cuit",
            new { Cuit = cuit });

        return Ok(new
        {
            cuit,
            codClient = row.CodClient?.Trim(),
            razonSoci = row.RazonSoci?.Trim(),
        });
    }

    private static string LockKey(string? tComp, string? nComp, DateTime fechaVto)
        => $"{tComp?.Trim()}|{nComp?.Trim()}|{fechaVto:yyyy-MM-dd}";

    private async Task<HashSet<string>> GetLockedComprobantesAsync(System.Data.IDbConnection conn, string cuit)
    {
        var lockedJson = await conn.QueryAsync<string>(@"
            SELECT Comprobantes
            FROM PagosCuentaCorriente
            WHERE Cuit = @Cuit AND EstadoPago IN ('Pendiente', 'Aprobado')",
            new { Cuit = cuit });

        var locked = new HashSet<string>();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        foreach (var json in lockedJson)
        {
            if (string.IsNullOrEmpty(json)) continue;
            try
            {
                var items = JsonSerializer.Deserialize<List<ComprobanteRef>>(json, jsonOptions);
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

    [HttpGet("pagos")]
    public async Task<IActionResult> GetPagos()
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var pagos = await conn.QueryAsync<PagoCuentaCorrienteDto>(
            @"SELECT Id, Cuit, Monto, Comprobantes, ExternalReference, PreferenceId, EstadoPago,
                     MpPaymentId, FechaPago, CreatedAt, MedioPago, CodigoBarra, FechaVencimiento
              FROM PagosCuentaCorriente
              WHERE Cuit = @Cuit
              ORDER BY CreatedAt DESC",
            new { Cuit = cuit });

        return Ok(pagos);
    }

    [HttpGet("pagos/{id}")]
    public async Task<IActionResult> GetPagoById(int id)
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var pago = await conn.QueryFirstOrDefaultAsync<PagoCuentaCorrienteDto>(
            @"SELECT p.Id, p.Cuit, p.Monto, p.Comprobantes, p.ExternalReference, p.PreferenceId, p.EstadoPago,
                     p.MpPaymentId, p.FechaPago, p.CreatedAt, p.MedioPago, p.CodigoBarra, p.FechaVencimiento,
                     c.RazonSoci
              FROM PagosCuentaCorriente p
              LEFT JOIN Clientes c ON c.Cuit = p.Cuit
              WHERE p.Id = @Id AND p.Cuit = @Cuit",
            new { Id = id, Cuit = cuit });

        if (pago == null)
            return NotFound(new { error = "Pago no encontrado" });

        return Ok(pago);
    }

    [HttpPost("generar-cupon-pagofacil")]
    public async Task<IActionResult> GenerarCuponPagoFacil([FromBody] GenerarPagoCuentaRequest request)
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        if (request.Ids == null || request.Ids.Length == 0)
            return BadRequest(new { error = "Debe seleccionar al menos un comprobante" });

        using var conn = _dbFactory.CreateConnection();
        var movimientos = (await conn.QueryAsync<ResumenCuentaDto>(
            "SELECT Id, Cuit, TComp, NComp, FechaVto, Saldo FROM ResumenCuenta WHERE Cuit = @Cuit AND Id IN @Ids ORDER BY FechaVto ASC",
            new { Cuit = cuit, request.Ids })).ToList();

        if (movimientos.Count == 0)
            return NotFound(new { error = "No se encontraron comprobantes" });

        var total = movimientos.Sum(m => m.Saldo);
        if (total <= 0)
            return BadRequest(new { error = "El total a pagar debe ser mayor a cero" });

        var externalReference = $"PF-{cuit}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var comprobantesJson = JsonSerializer.Serialize(
            movimientos.Select(m => new { m.TComp, m.NComp, m.FechaVto, m.Saldo }));
        var fechaVencimiento = DateTime.UtcNow.AddDays(30);

        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // Insertamos primero sin codigo de barra para obtener el Id auto-incremental.
            // El codigo Pago Facil incluye el id desplazado +900000 en la "cuenta",
            // asi que no se puede calcular antes del insert.
            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO PagosCuentaCorriente
                    (Cuit, Monto, Comprobantes, ExternalReference, EstadoPago, MedioPago, FechaVencimiento, CreatedAt, UpdatedAt)
                VALUES
                    (@Cuit, @Monto, @Comprobantes, @ExternalReference, 'Pendiente', 'PagoFacil', @FechaVencimiento, UTC_TIMESTAMP(), UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();",
                new
                {
                    Cuit = cuit,
                    Monto = total,
                    Comprobantes = comprobantesJson,
                    ExternalReference = externalReference,
                    FechaVencimiento = fechaVencimiento,
                }, tx);

            var codigoBarra = PagoFacilCodigo.Generar(id, total, fechaVencimiento);

            await conn.ExecuteAsync(
                "UPDATE PagosCuentaCorriente SET CodigoBarra = @Codigo WHERE Id = @Id",
                new { Codigo = codigoBarra, Id = id }, tx);

            tx.Commit();

            _logger.LogInformation(
                "Cupon Pago Facil generado: CUIT={Cuit}, Id={Id}, Monto={Total}, Vto={Vto:yyyy-MM-dd}",
                cuit, id, total, fechaVencimiento);

            return Ok(new GenerarCuponPagoFacilResult
            {
                Id = id,
                ExternalReference = externalReference,
                Total = total,
                CodigoBarra = codigoBarra,
                FechaVencimiento = fechaVencimiento,
            });
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    [HttpPost("generar-pago")]
    public async Task<IActionResult> GenerarPago([FromBody] GenerarPagoCuentaRequest request)
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        if (request.Ids == null || request.Ids.Length == 0)
            return BadRequest(new { error = "Debe seleccionar al menos un comprobante" });

        using var conn = _dbFactory.CreateConnection();
        var movimientos = (await conn.QueryAsync<ResumenCuentaDto>(
            "SELECT Id, Cuit, TComp, NComp, FechaVto, Saldo FROM ResumenCuenta WHERE Cuit = @Cuit AND Id IN @Ids ORDER BY FechaVto ASC",
            new { Cuit = cuit, request.Ids })).ToList();

        if (movimientos.Count == 0)
            return NotFound(new { error = "No se encontraron comprobantes" });

        var total = movimientos.Sum(m => m.Saldo);
        if (total <= 0)
            return BadRequest(new { error = "El total a pagar debe ser mayor a cero" });

        var externalReference = $"CC-{cuit}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var frontendBaseUrl = await _mpService.EnsureConfiguradoAsync();

        var comprobantesDesc = string.Join(", ", movimientos.Select(m => $"{m.TComp} {m.NComp}"));
        var comprobantesJson = JsonSerializer.Serialize(movimientos.Select(m => new { m.TComp, m.NComp, m.FechaVto, m.Saldo }));

        var client = new PreferenceClient();
        var preferenceRequest = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
            {
                new PreferenceItemRequest
                {
                    Title = "SAD - Cuenta Corriente",
                    Description = $"Comprobantes: {comprobantesDesc}",
                    Quantity = 1,
                    CurrencyId = "ARS",
                    UnitPrice = total,
                }
            },
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = $"{frontendBaseUrl}/resumen-cuenta?status=approved",
                Failure = $"{frontendBaseUrl}/resumen-cuenta?status=rejected",
                Pending = $"{frontendBaseUrl}/resumen-cuenta?status=pending",
            },
            AutoReturn = "approved",
            ExternalReference = externalReference,
        };

        _logger.LogInformation("Creando preferencia MP para cuenta corriente CUIT={Cuit}, monto={Total}", cuit, total);

        Preference preference = await client.CreateAsync(preferenceRequest);

        // Registrar el pago en la base
        await conn.ExecuteAsync(@"
            INSERT INTO PagosCuentaCorriente (Cuit, Monto, Comprobantes, ExternalReference, PreferenceId, EstadoPago, CreatedAt, UpdatedAt)
            VALUES (@Cuit, @Monto, @Comprobantes, @ExternalReference, @PreferenceId, 'Pendiente', UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            new
            {
                Cuit = cuit,
                Monto = total,
                Comprobantes = comprobantesJson,
                ExternalReference = externalReference,
                PreferenceId = preference.Id,
            });

        _logger.LogInformation("Pago CC registrado: Ref={Ref}, PreferenceId={Id}", externalReference, preference.Id);

        return Ok(new
        {
            preferenceId = preference.Id,
            initPoint = preference.InitPoint,
            total,
            externalReference,
        });
    }

    [HttpPost("validar-pendientes")]
    public async Task<IActionResult> ValidarPendientes()
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        using var conn = _dbFactory.CreateConnection();

        // Cleanup: 30 dias para cupones Pago Facil (su FechaVencimiento es a 30d), 7 dias para el resto (MP).
        var eliminados = await conn.ExecuteAsync(@"
            DELETE FROM PagosCuentaCorriente
            WHERE Cuit = @Cuit
              AND EstadoPago = 'Pendiente'
              AND (
                (MedioPago = 'PagoFacil' AND CreatedAt < DATE_SUB(UTC_TIMESTAMP(), INTERVAL 30 DAY))
                OR (COALESCE(MedioPago, '') <> 'PagoFacil' AND CreatedAt < DATE_SUB(UTC_TIMESTAMP(), INTERVAL 7 DAY))
              )",
            new { Cuit = cuit });

        // Solo consultamos MP por pagos que no son Pago Facil — los PF se confirman manualmente.
        var pendientes = (await conn.QueryAsync<PagoCuentaCorrienteDto>(@"
            SELECT Id, Cuit, Monto, Comprobantes, ExternalReference, PreferenceId, EstadoPago, MpPaymentId, FechaPago, CreatedAt
            FROM PagosCuentaCorriente
            WHERE Cuit = @Cuit
              AND EstadoPago = 'Pendiente'
              AND COALESCE(MedioPago, '') <> 'PagoFacil'
              AND CreatedAt >= DATE_SUB(UTC_TIMESTAMP(), INTERVAL 7 DAY)",
            new { Cuit = cuit })).ToList();

        var aprobados = 0;
        var rechazados = 0;

        foreach (var pago in pendientes)
        {
            MercadoPagoPaymentInfo? mpInfo;
            try
            {
                mpInfo = await _mpService.BuscarPagoPorReferenciaAsync(pago.ExternalReference);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error consultando MP para pago {Id} ref={Ref}", pago.Id, pago.ExternalReference);
                continue;
            }

            if (mpInfo == null) continue;

            var nuevoEstado = mpInfo.Status switch
            {
                "approved" => "Aprobado",
                "rejected" => "Rechazado",
                _ => "Pendiente"
            };

            if (nuevoEstado == "Pendiente") continue;

            await conn.ExecuteAsync(@"
                UPDATE PagosCuentaCorriente
                SET EstadoPago = @Estado,
                    MpPaymentId = @MpId,
                    FechaPago = CASE WHEN @Estado = 'Aprobado' THEN UTC_TIMESTAMP() ELSE FechaPago END,
                    UpdatedAt = UTC_TIMESTAMP()
                WHERE Id = @Id",
                new { Estado = nuevoEstado, MpId = mpInfo.Id, Id = pago.Id });

            if (nuevoEstado == "Aprobado") aprobados++;
            else rechazados++;
        }

        _logger.LogInformation(
            "ValidarPendientes CUIT={Cuit}: eliminados={Del}, revisados={Rev}, aprobados={Apr}, rechazados={Rech}",
            cuit, eliminados, pendientes.Count, aprobados, rechazados);

        return Ok(new
        {
            eliminados,
            revisados = pendientes.Count,
            aprobados,
            rechazados,
            pendientes = pendientes.Count - aprobados - rechazados,
        });
    }

    [HttpPost("confirmar-pago")]
    public async Task<IActionResult> ConfirmarPago([FromBody] ConfirmarPagoCCRequest request)
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        if (string.IsNullOrEmpty(request.ExternalReference) && request.PaymentId == null)
            return BadRequest(new { error = "Se requiere externalReference o paymentId" });

        using var conn = _dbFactory.CreateConnection();

        // Buscar el pago registrado
        PagoCuentaCorrienteDto? pago = null;
        if (!string.IsNullOrEmpty(request.ExternalReference))
        {
            pago = await conn.QueryFirstOrDefaultAsync<PagoCuentaCorrienteDto>(
                "SELECT * FROM PagosCuentaCorriente WHERE ExternalReference = @Ref AND Cuit = @Cuit",
                new { Ref = request.ExternalReference, Cuit = cuit });
        }

        if (pago == null)
            return NotFound(new { error = "No se encontró el registro de pago" });

        if (pago.EstadoPago == "Aprobado")
            return Ok(new { estadoPago = "Aprobado", message = "El pago ya fue confirmado anteriormente" });

        // Consultar estado en MP
        MercadoPagoPaymentInfo? mpInfo = null;
        if (request.PaymentId.HasValue)
        {
            mpInfo = await _mpService.ObtenerInfoPagoAsync(request.PaymentId.Value);
        }
        else if (!string.IsNullOrEmpty(request.ExternalReference))
        {
            mpInfo = await _mpService.BuscarPagoPorReferenciaAsync(request.ExternalReference);
        }

        var estadoFinal = "Pendiente";
        if (mpInfo != null)
        {
            estadoFinal = mpInfo.Status switch
            {
                "approved" => "Aprobado",
                "rejected" => "Rechazado",
                _ => "Pendiente"
            };

            await conn.ExecuteAsync(@"
                UPDATE PagosCuentaCorriente
                SET EstadoPago = @Estado, MpPaymentId = @MpId, FechaPago = CASE WHEN @Estado = 'Aprobado' THEN UTC_TIMESTAMP() ELSE FechaPago END, UpdatedAt = UTC_TIMESTAMP()
                WHERE Id = @Id",
                new { Estado = estadoFinal, MpId = mpInfo.Id, Id = pago.Id });
        }

        return Ok(new { estadoPago = estadoFinal, monto = pago.Monto });
    }

    // --- ADMIN ---

    [HttpGet("admin/pagos")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AdminGetAllPagos()
    {
        using var conn = _dbFactory.CreateConnection();
        var pagos = await conn.QueryAsync<PagoCuentaCorrienteDto>(
            @"SELECT Id, Cuit, Monto, Comprobantes, ExternalReference, PreferenceId, EstadoPago,
                     MpPaymentId, FechaPago, CreatedAt, MedioPago, CodigoBarra, FechaVencimiento
              FROM PagosCuentaCorriente
              ORDER BY CreatedAt DESC");

        return Ok(pagos);
    }

    [HttpPost("admin/pagos/{id}/aprobar")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AdminAprobarPago(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        var pago = await conn.QueryFirstOrDefaultAsync<PagoCuentaCorrienteDto>(
            "SELECT * FROM PagosCuentaCorriente WHERE Id = @Id", new { Id = id });

        if (pago == null)
            return NotFound(new { error = "Pago no encontrado" });

        await conn.ExecuteAsync(@"
            UPDATE PagosCuentaCorriente
            SET EstadoPago = 'Aprobado', FechaPago = UTC_TIMESTAMP(), UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id", new { Id = id });

        _logger.LogInformation("Admin aprobó pago CC Id={Id}, CUIT={Cuit}, Monto={Monto}", id, pago.Cuit, pago.Monto);

        return Ok(new { message = "Pago marcado como aprobado" });
    }

    [HttpPost("admin/pagos/{id}/rechazar")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AdminRechazarPago(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        var pago = await conn.QueryFirstOrDefaultAsync<PagoCuentaCorrienteDto>(
            "SELECT * FROM PagosCuentaCorriente WHERE Id = @Id", new { Id = id });

        if (pago == null)
            return NotFound(new { error = "Pago no encontrado" });

        await conn.ExecuteAsync(@"
            UPDATE PagosCuentaCorriente
            SET EstadoPago = 'Rechazado', UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id", new { Id = id });

        _logger.LogInformation("Admin rechazó pago CC Id={Id}, CUIT={Cuit}", id, pago.Cuit);

        return Ok(new { message = "Pago marcado como rechazado" });
    }

    [HttpPost("admin/pagos/{id}/verificar-mp")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AdminVerificarMP(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        var pago = await conn.QueryFirstOrDefaultAsync<PagoCuentaCorrienteDto>(
            "SELECT * FROM PagosCuentaCorriente WHERE Id = @Id", new { Id = id });

        if (pago == null)
            return NotFound(new { error = "Pago no encontrado" });

        var mpInfo = await _mpService.BuscarPagoPorReferenciaAsync(pago.ExternalReference);

        if (mpInfo == null)
            return Ok(new { encontrado = false, message = "No se encontró pago en Mercado Pago para esta referencia" });

        var estadoFinal = mpInfo.Status switch
        {
            "approved" => "Aprobado",
            "rejected" => "Rechazado",
            _ => "Pendiente"
        };

        await conn.ExecuteAsync(@"
            UPDATE PagosCuentaCorriente
            SET EstadoPago = @Estado, MpPaymentId = @MpId, FechaPago = CASE WHEN @Estado = 'Aprobado' THEN UTC_TIMESTAMP() ELSE FechaPago END, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id",
            new { Estado = estadoFinal, MpId = mpInfo.Id, Id = pago.Id });

        return Ok(new { encontrado = true, estadoPago = estadoFinal, mpPaymentId = mpInfo.Id, mpStatus = mpInfo.Status });
    }
}

public class ResumenCuentaDto
{
    public int Id { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public string TComp { get; set; } = string.Empty;
    public string NComp { get; set; } = string.Empty;
    public DateTime FechaVto { get; set; }
    public decimal Saldo { get; set; }
}

public class PagoCuentaCorrienteDto
{
    public int Id { get; set; }
    public string Cuit { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Comprobantes { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string? PreferenceId { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public long? MpPaymentId { get; set; }
    public DateTime? FechaPago { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? MedioPago { get; set; }
    public string? CodigoBarra { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? RazonSoci { get; set; }
}

public class GenerarCuponPagoFacilResult
{
    public int Id { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string CodigoBarra { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
}

public class GenerarPagoCuentaRequest
{
    public int[] Ids { get; set; } = [];
}

public class ConfirmarPagoCCRequest
{
    public long? PaymentId { get; set; }
    public string? ExternalReference { get; set; }
}

internal class ComprobanteRef
{
    public string? TComp { get; set; }
    public string? NComp { get; set; }
    public DateTime FechaVto { get; set; }
}
