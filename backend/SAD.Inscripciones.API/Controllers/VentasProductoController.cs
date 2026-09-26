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
}
