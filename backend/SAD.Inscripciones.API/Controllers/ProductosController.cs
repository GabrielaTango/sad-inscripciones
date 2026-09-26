using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _service;

    public ProductosController(IProductoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetPublicosAsync());
    }

    [HttpGet("admin")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetAllAdmin()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("admin/{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetByIdAdmin(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetPublicoByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create([FromBody] ProductoCreateDto dto)
    {
        var id = await _service.CreateAsync(dto);
        var creado = await _service.GetByIdAsync(id);
        return CreatedAtAction(nameof(GetByIdAdmin), new { id }, creado);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await _service.DeleteAsync(id);
        return Ok(new { deleted = resultado.ToString() });
    }
}
