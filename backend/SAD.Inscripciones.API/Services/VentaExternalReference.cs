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

    /// <summary>
    /// Extrae ventaId y publicRef de un external_reference ("venta-12-abc123def...").
    /// El llamador todavía debe comparar publicRef contra el de la venta cargada por
    /// ventaId antes de confirmar el pago (ver <c>VentaProductoService.ProcesarPagoAsync</c>).
    /// </summary>
    public static bool TryParse(string? externalReference, out int ventaId, out string publicRef)
    {
        ventaId = 0;
        publicRef = string.Empty;
        if (string.IsNullOrEmpty(externalReference) || !externalReference.StartsWith(Prefix))
            return false;

        var resto = externalReference[Prefix.Length..];
        var partes = resto.Split('-', 2);
        if (partes.Length != 2 || string.IsNullOrEmpty(partes[1]))
            return false;

        publicRef = partes[1];
        return int.TryParse(partes[0], out ventaId);
    }
}
