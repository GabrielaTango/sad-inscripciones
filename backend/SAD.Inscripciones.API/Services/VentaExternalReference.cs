namespace SAD.Inscripciones.API.Services;

/// <summary>
/// Arma y parsea el external_reference que se manda a MercadoPago para ventas de
/// producto. Formato: "venta-{ventaId}-{publicRef}". El prefijo "venta-" es lo que
/// distingue estas referencias de las de inscripciones (ver ExternalReferenceHelper):
/// el parseo de inscripciones hace int.Parse sobre el primer segmento y falla con
/// este prefijo, así que el webhook de inscripciones ignora estos pagos.
/// </summary>
public static class VentaExternalReference
{
    private const string Prefix = "venta-";

    public static string Build(int ventaId, string publicRef) => $"{Prefix}{ventaId}-{publicRef}";

    // Extrae el ventaId de un external_reference ("venta-12-abc123def...").
    public static bool TryParseVentaId(string? externalReference, out int ventaId)
    {
        ventaId = 0;
        if (string.IsNullOrEmpty(externalReference) || !externalReference.StartsWith(Prefix))
            return false;

        var resto = externalReference[Prefix.Length..];
        var idPart = resto.Split('-', 2)[0];
        return int.TryParse(idPart, out ventaId);
    }
}
