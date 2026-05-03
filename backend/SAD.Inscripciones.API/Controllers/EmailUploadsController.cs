using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/admin/email-uploads")]
[Authorize(Policy = "Admin")]
public class EmailUploadsController : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> AllowedMime = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml",
    };
    private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg",
    };

    private readonly IWebHostEnvironment _env;

    public EmailUploadsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Upload de imágenes para el editor de templates Unlayer.
    /// Devuelve { url } absoluta al recurso servido bajo /uploads/email/{guid}.{ext}.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Archivo vacío." });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = $"Archivo excede el máximo de {MaxBytes / 1024 / 1024} MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext) || !AllowedMime.Contains(file.ContentType))
            return BadRequest(new { message = "Tipo de archivo no permitido." });

        var dir = Path.Combine(_env.WebRootPath, "uploads", "email");
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, name);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        // URL absoluta: necesario porque los mails se ven fuera del dominio.
        var url = $"{Request.Scheme}://{Request.Host}/uploads/email/{name}";
        return Ok(new { url });
    }
}
