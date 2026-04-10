using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsuariosController(IUsuarioService service)
    {
        _service = service;
    }

    private string GetCurrentUser() => User.FindFirst("cuit")?.Value ?? "system";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await _service.GetAllAsync();
        return Ok(usuarios.Select(u => new
        {
            u.Id,
            u.Username,
            u.NombreCompleto,
            u.Email,
            u.Activo,
            u.CreatedAt,
            u.UpdatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var u = await _service.GetByIdAsync(id);
        return Ok(new
        {
            u.Id,
            u.Username,
            u.NombreCompleto,
            u.Email,
            u.Activo,
            u.CreatedAt,
            u.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UsuarioCreateDto dto)
    {
        var entity = new Usuario
        {
            Username = dto.Username,
            NombreCompleto = dto.NombreCompleto,
            Email = dto.Email,
            Activo = dto.Activo,
            CreatedBy = GetCurrentUser(),
            UpdatedBy = GetCurrentUser()
        };

        var id = await _service.CreateAsync(entity, dto.Password);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            entity.Id,
            entity.Username,
            entity.NombreCompleto,
            entity.Email,
            entity.Activo
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDto dto)
    {
        var entity = new Usuario
        {
            Id = id,
            Username = dto.Username,
            NombreCompleto = dto.NombreCompleto,
            Email = dto.Email,
            Activo = dto.Activo,
            UpdatedBy = GetCurrentUser()
        };

        await _service.UpdateAsync(entity);
        return NoContent();
    }

    [HttpPut("{id}/password")]
    public async Task<IActionResult> CambiarPassword(int id, [FromBody] CambiarPasswordDto dto)
    {
        await _service.CambiarPasswordAsync(id, dto.PasswordActual, dto.PasswordNueva, GetCurrentUser());
        return NoContent();
    }

    [HttpPut("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        await _service.ResetPasswordAsync(id, dto.PasswordNueva, GetCurrentUser());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, GetCurrentUser());
        return NoContent();
    }
}
