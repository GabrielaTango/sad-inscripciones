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
