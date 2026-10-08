using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/ventas-producto")]
public class VentasProductoController : ControllerBase
{
    private readonly IVentaProductoService _service;

    public VentasProductoController(IVentaProductoService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VentaProductoCreateDto dto)
    {
        var resultado = await _service.CrearAsync(dto);
        return Ok(resultado);
    }

    /// <summary>
    /// Listado admin de ventas. Sin "estado" (o con "Todas", sin distinguir
    /// mayusculas) devuelve todos los estados.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? productoId,
        [FromQuery] string? estado,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? texto)
    {
        return Ok(await _service.ListAdminAsync(productoId, ResolverEstado(estado), desde, hasta, texto));
    }

    [HttpGet("export")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Export(
        [FromQuery] int? productoId,
        [FromQuery] string? estado,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? texto)
    {
        var bytes = await _service.ExportToExcelAsync(productoId, ResolverEstado(estado), desde, hasta, texto);
        var nombre = $"ventas-producto_{DateTime.Now:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
    }

    private static string? ResolverEstado(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado) || string.Equals(estado, "Todas", StringComparison.OrdinalIgnoreCase))
            return null;
        return estado;
    }

    /// <summary>
    /// Consulta en MercadoPago todas las ventas Pendiente y devuelve el resumen
    /// (consultadas, pagadas, impagas, siguen pendientes, errores).
    /// </summary>
    [HttpPost("consultar-pendientes")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ConsultarPendientes()
    {
        return Ok(await _service.ConsultarPendientesAsync());
    }

    /// <summary>
    /// Llamado por la página pública de resultado del pago: si la venta sigue
    /// Pendiente, busca los pagos en MercadoPago por external_reference y confirma
    /// los aprobados. Sirve como respaldo del webhook dedicado (por si la
    /// notificación de MP no llegó a tiempo o el backend no tiene un host público
    /// configurado para recibirla).
    /// </summary>
    [HttpPost("{publicRef}/verificar")]
    public async Task<IActionResult> Verificar(string publicRef)
    {
        var resultado = await _service.VerificarAsync(publicRef);
        return Ok(resultado);
    }
}
