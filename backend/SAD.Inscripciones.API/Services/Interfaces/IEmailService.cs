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
    /// Dispara el mail de reserva pagada (cuando la inscripcion pasa a estado "Reservada":
    /// el alumno abonó el MontoReserva pero todavía debe el saldo). Mismo contrato que
    /// la confirmación: no tira si falla.
    /// </summary>
    Task EnviarReservaPagadaAsync(Inscripcion inscripcion);

    /// <summary>
    /// Envía el mail de confirmación de una venta de producto, usando el asunto y
    /// cuerpo configurados en el propio Producto (MailAsunto/MailCuerpoHtml, no
    /// EmailTemplates) con reemplazo de {{Nombre}}, {{Apellido}}, {{Dni}}, {{Email}},
    /// {{Producto}}, {{Importe}} y una entrada por cada clave de DatosExtra. Si el
    /// email está deshabilitado o el producto no tiene asunto/cuerpo configurados, no
    /// envía nada. No tira excepción si falla: loguea y devuelve false. Devuelve true
    /// solo si el mail se envió efectivamente.
    /// </summary>
    Task<bool> EnviarConfirmacionVentaAsync(VentaProducto venta, Producto producto);

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

    /// <summary>
    /// Envía la consulta del formulario público de contacto al email destino
    /// configurado en ConfiguracionContacto. No propaga errores: loguea y sigue,
    /// para no abortar la persistencia del contacto en DB.
    /// </summary>
    Task EnviarConsultaContactoAsync(Contacto contacto);

    /// <summary>Invalida el cache de ConfiguracionContacto.</summary>
    void InvalidarCacheContacto();
}
