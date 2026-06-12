namespace SAD.Inscripciones.API.Services;

/// <summary>
/// Arma y parsea el external_reference que se manda a MercadoPago para inscripciones.
/// Formato: "{inscripcionId}-{publicRef}". El token (PublicRef) evita que un pago viejo
/// del sandbox de MP reaparezca cuando se reusan ids de inscripción (al recrear la base):
/// la búsqueda por referencia solo trae los pagos de ESTA inscripción.
/// Para inscripciones legacy sin token, cae al id pelado (comportamiento anterior).
/// </summary>
public static class ExternalReferenceHelper
{
    public static string Build(int inscripcionId, string? publicRef) =>
        string.IsNullOrEmpty(publicRef) ? inscripcionId.ToString() : $"{inscripcionId}-{publicRef}";

    // Extrae el inscripcionId de un external_reference ("58-abc123" o "58").
    public static bool TryParseInscripcionId(string? externalReference, out int inscripcionId)
    {
        inscripcionId = 0;
        if (string.IsNullOrEmpty(externalReference)) return false;
        var idPart = externalReference.Split('-', 2)[0];
        return int.TryParse(idPart, out inscripcionId);
    }
}
