namespace SyncService.Helpers;

/// <summary>
/// Constantes para consultar la tabla de clientes de Tango (GVA14).
///
/// Un mismo CUIT puede tener varias filas en GVA14 (cliente re-creado): la fila
/// habilitada lleva <c>FECHA_INHA = '1800-01-01'</c> y las inhabilitadas una fecha real,
/// cada una con su propio COD_CLIENT. Todo lookup por CUIT debe filtrar por
/// <see cref="Activo"/> y, ante más de una activa, quedarse con el mayor ID_GVA14.
/// </summary>
public static class Gva14
{
    /// <summary>Predicado SQL que selecciona sólo la fila habilitada del cliente.</summary>
    public const string Activo = "FECHA_INHA = '1800-01-01'";
}
