using System.Security.Claims;
using System.Text.Json;
using Dapper;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/resumen-cuenta")]
[Authorize]
public class ResumenCuentaController : ControllerBase
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly IMercadoPagoService _mpService;
    private readonly ILogger<ResumenCuentaController> _logger;

    public ResumenCuentaController(DbConnectionFactory dbFactory, IConfiguration configuration, IMercadoPagoService mpService, ILogger<ResumenCuentaController> logger)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
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
        var movimientos = await conn.QueryAsync<ResumenCuentaDto>(
            "SELECT Id, Cuit, TComp, NComp, FechaVto, Saldo FROM ResumenCuenta WHERE Cuit = @Cuit ORDER BY FechaVto ASC",
            new { Cuit = cuit });

        return Ok(movimientos);
    }

    [HttpGet("pagos")]
    public async Task<IActionResult> GetPagos()
    {
        var cuit = GetCuit();
        if (string.IsNullOrEmpty(cuit))
            return Unauthorized();

        using var conn = _dbFactory.CreateConnection();
        var pagos = await conn.QueryAsync<PagoCuentaCorrienteDto>(
            @"SELECT Id, Cuit, Monto, Comprobantes, ExternalReference, PreferenceId, EstadoPago, MpPaymentId, FechaPago, CreatedAt
              FROM PagosCuentaCorriente
              WHERE Cuit = @Cuit
              ORDER BY CreatedAt DESC",
            new { Cuit = cuit });

        return Ok(pagos);
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
        var frontendBaseUrl = _configuration["MercadoPago:FrontendBaseUrl"] ?? "http://localhost:5173";

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
            @"SELECT Id, Cuit, Monto, Comprobantes, ExternalReference, PreferenceId, EstadoPago, MpPaymentId, FechaPago, CreatedAt
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
    public string Comprobantes { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string? PreferenceId { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public long? MpPaymentId { get; set; }
    public DateTime? FechaPago { get; set; }
    public DateTime CreatedAt { get; set; }
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
