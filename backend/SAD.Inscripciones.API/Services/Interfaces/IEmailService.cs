using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Dispara el mail de confirmación. No tira excepción si falla:
    /// loguea y sigue, para no abortar la transacción de confirmación.
    /// </summary>
    Task EnviarConfirmacionInscripcionAsync(Inscripcion inscripcion);

    /// <summary>
    /// Envía un mail de prueba con la config actual al destinatario indicado.
    /// SÍ propaga errores (lo usa el panel admin para diagnóstico).
    /// </summary>
    Task EnviarPruebaAsync(string destinatario);

    /// <summary>
    /// Envía un template ad-hoc (HTML + asunto) con variables de muestra al destinatario.
    /// Usado por el editor de templates para probar cambios antes de guardar.
    /// </summary>
    Task EnviarPruebaTemplateAsync(string destinatario, string asunto, string bodyHtml);

    /// <summary>Invalida el cache de configuración (después de un PUT).</summary>
    void InvalidarCache();
}
