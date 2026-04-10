using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactoController : ControllerBase
{
    private readonly IContactoRepository _repository;

    public ContactoController(IContactoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var contactos = await _repository.GetAllAsync();
        return Ok(contactos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contacto = await _repository.GetByIdAsync(id);
        if (contacto == null)
            return NotFound();
        return Ok(contacto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactoDto dto)
    {
        var contacto = new Contacto
        {
            Nombre = dto.Nombre,
            Email = dto.Email,
            Asunto = dto.Asunto,
            Mensaje = dto.Mensaje,
            FechaEnvio = DateTime.UtcNow,
            Leido = false
        };

        var id = await _repository.CreateAsync(contacto);
        contacto.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, contacto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContactoDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Nombre = dto.Nombre;
        existing.Email = dto.Email;
        existing.Asunto = dto.Asunto;
        existing.Mensaje = dto.Mensaje;

        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
