using System.Globalization;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class EmailService : IEmailService
{
    public const string TemplateInscripcionConfirmada = "inscripcion-confirmada";
    private const string SeedFileFor = "inscripcion-confirmada"; // si la DB está vacía, sembramos desde EmailTemplates/{codigo}.html
    private static readonly TimeSpan ConfigCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICryptoService _crypto;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _env;

    private ConfiguracionEmail? _cachedConfig;
    private DateTime _cachedAt;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public EmailService(
        IServiceScopeFactory scopeFactory,
        ICryptoService crypto,
        ILogger<EmailService> logger,
        IWebHostEnvironment env)
    {
        _scopeFactory = scopeFactory;
        _crypto = crypto;
        _logger = logger;
        _env = env;
    }

    public async Task EnviarConfirmacionInscripcionAsync(Inscripcion inscripcion)
    {
        try
        {
            var config = await GetConfigAsync();
            if (!config.Activo)
            {
                _logger.LogInformation("Email deshabilitado en config; no se envía confirmación de inscripción {Id}", inscripcion.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(inscripcion.Email))
            {
                _logger.LogWarning("Inscripción {Id} sin Email; no se envía confirmación.", inscripcion.Id);
                return;
            }

            Evento? evento;
            using (var scope = _scopeFactory.CreateScope())
            {
                var eventoRepo = scope.ServiceProvider.GetRequiredService<IEventoRepository>();
                evento = await eventoRepo.GetByIdAsync(inscripcion.EventoId);
            }

            var variables = BuildVariables(inscripcion, evento);
            var template = await LoadTemplateAsync(TemplateInscripcionConfirmada);

            if (!template.Activo)
            {
                _logger.LogInformation("Template {Codigo} inactivo; no se envía mail para inscripción {Id}", TemplateInscripcionConfirmada, inscripcion.Id);
                return;
            }

            var asunto = ReplaceVariables(template.Asunto, variables);
            var body = ReplaceVariables(template.BodyHtml, variables);

            await SendAsync(config, inscripcion.Email, asunto, body);
            _logger.LogInformation("Mail de confirmación enviado a {Email} para inscripción {Id}", inscripcion.Email, inscripcion.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló envío de confirmación para inscripción {Id} ({Email})", inscripcion.Id, inscripcion.Email);
        }
    }

    public async Task EnviarPruebaAsync(string destinatario)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ArgumentException("Destinatario requerido.", nameof(destinatario));

        var config = await GetConfigAsync();
        var asunto = "[Prueba] Configuración SMTP - SAD Inscripciones";
        var body = $@"<html><body style='font-family:Arial,sans-serif'>
            <h2>Configuración SMTP OK</h2>
            <p>Este es un mail de prueba enviado desde el panel admin de SAD Inscripciones.</p>
            <p><strong>Servidor:</strong> {System.Net.WebUtility.HtmlEncode(config.Host)}:{config.Port} (SSL: {config.EnableSsl})</p>
            <p><strong>Fecha:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
            </body></html>";

        await SendAsync(config, destinatario, asunto, body);
    }

    public async Task EnviarPruebaTemplateAsync(string destinatario, string asunto, string bodyHtml)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ArgumentException("Destinatario requerido.", nameof(destinatario));

        var config = await GetConfigAsync();
        var vars = SampleVariables();
        await SendAsync(
            config,
            destinatario,
            "[Prueba] " + ReplaceVariables(asunto, vars),
            ReplaceVariables(bodyHtml, vars));
    }

    private static Dictionary<string, string> SampleVariables() => new()
    {
        ["Nombre"] = "Juan",
        ["Apellido"] = "Pérez",
        ["Evento"] = "Congreso SAD 2026",
        ["Lugar"] = "Hotel Sheraton, CABA",
        ["FechaEvento"] = "15/08/2026",
        ["Importe"] = "$ 25.000,00",
        ["Cuotas"] = "3",
        ["NumeroInscripcion"] = "1234",
    };

    public void InvalidarCache()
    {
        _cachedConfig = null;
    }

    // ---------- internals ----------

    private async Task SendAsync(ConfiguracionEmail config, string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(config.Host))
            throw new InvalidOperationException("Host SMTP no configurado.");
        if (string.IsNullOrWhiteSpace(config.FromEmail))
            throw new InvalidOperationException("FromEmail no configurado.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));

        if (!string.IsNullOrWhiteSpace(config.ReplyTo))
            message.ReplyTo.Add(MailboxAddress.Parse(config.ReplyTo));

        if (!string.IsNullOrWhiteSpace(config.BccCopia))
        {
            foreach (var bcc in config.BccCopia.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                message.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        message.Subject = subject;
        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();

        if (config.IgnorarCertificadoSsl)
        {
            // Hosting compartido: el cert suele ser del hostname físico del VPS
            // y no del dominio. Se acepta cualquier cert solo cuando el admin lo activa.
            smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        // Auto se equivoca seguido: elegimos explícito según puerto.
        // 465 = SSL implícito desde el inicio. 587/25 con SSL = STARTTLS.
        var secure = (config.EnableSsl, config.Port) switch
        {
            (true, 465) => SecureSocketOptions.SslOnConnect,
            (true, _) => SecureSocketOptions.StartTls,
            (false, _) => SecureSocketOptions.None,
        };
        try {
            await smtp.ConnectAsync(config.Host, config.Port, secure);
        } catch(Exception ex)
        {
            var x = ex.Message;
            throw;
        }

        if (!string.IsNullOrEmpty(config.Usuario))
        {
            var password = _crypto.Decrypt(config.PasswordCifrado);
            await smtp.AuthenticateAsync(config.Usuario, password);
        }

        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    private async Task<ConfiguracionEmail> GetConfigAsync()
    {
        if (_cachedConfig != null && DateTime.UtcNow - _cachedAt < ConfigCacheTtl)
            return _cachedConfig;

        await _cacheLock.WaitAsync();
        try
        {
            if (_cachedConfig != null && DateTime.UtcNow - _cachedAt < ConfigCacheTtl)
                return _cachedConfig;

            using var scope = _scopeFactory.CreateScope();
            var configRepo = scope.ServiceProvider.GetRequiredService<IConfiguracionEmailRepository>();
            _cachedConfig = await configRepo.GetAsync();
            _cachedAt = DateTime.UtcNow;
            return _cachedConfig;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Carga template de la DB. Si BodyHtml está vacío, lo siembra una sola vez desde
    /// EmailTemplates/{codigo}.html del filesystem (el HTML que dejamos versionado en git).
    /// </summary>
    private async Task<EmailTemplate> LoadTemplateAsync(string codigo)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmailTemplateRepository>();
        var template = await repo.GetByCodigoAsync(codigo)
            ?? throw new InvalidOperationException($"No existe template de mail con código '{codigo}'.");

        if (string.IsNullOrWhiteSpace(template.BodyHtml))
        {
            var seedPath = Path.Combine(_env.ContentRootPath, "EmailTemplates", $"{codigo}.html");
            if (File.Exists(seedPath))
            {
                template.BodyHtml = await File.ReadAllTextAsync(seedPath);
                await repo.UpdateAsync(template);
                _logger.LogInformation("Template {Codigo} sembrado desde {Path}", codigo, seedPath);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Template '{codigo}' no tiene BodyHtml en DB y no existe seed en {seedPath}.");
            }
        }

        return template;
    }

    private static Dictionary<string, string> BuildVariables(Inscripcion insc, Evento? evento)
    {
        var ar = CultureInfo.GetCultureInfo("es-AR");
        return new Dictionary<string, string>
        {
            ["Nombre"] = insc.Nombre ?? string.Empty,
            ["Apellido"] = insc.Apellido ?? string.Empty,
            ["Evento"] = evento?.Titulo ?? "(evento)",
            ["Lugar"] = evento?.Lugar ?? string.Empty,
            ["FechaEvento"] = evento != null ? evento.FechaInicio.ToString("dd/MM/yyyy") : string.Empty,
            ["Importe"] = insc.PrecioFinal.ToString("C2", ar),
            ["Cuotas"] = insc.CantidadCuotas?.ToString() ?? "1",
            ["NumeroInscripcion"] = insc.Id.ToString(),
        };
    }

    private static string ReplaceVariables(string template, Dictionary<string, string> vars)
    {
        var result = template;
        foreach (var kvp in vars)
            result = result.Replace("{{" + kvp.Key + "}}", kvp.Value);
        return result;
    }
}
