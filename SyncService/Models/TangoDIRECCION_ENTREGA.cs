using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>
/// Dirección de entrega del cliente. Carga defaults de Config/DIRECCION_ENTREGA.xml.
/// </summary>
public class TangoDIRECCION_ENTREGA : TangoEntity
{
    protected override string TableName => "DIRECCION_ENTREGA";
    protected override string XmlFileName => "DIRECCION_ENTREGA.xml";

    public TangoDIRECCION_ENTREGA()
    {
        // Estos campos no están en el XML pero sí en la tabla; los inicializamos
        // como placeholder y los reemplazamos en FormatValue por subqueries.
        Set("ID_GVA14", "-1");
        Set("ID_GVA18", "-1");
    }

    protected override string FormatValue(string field, string value)
    {
        return field switch
        {
            "ID_DIRECCION_ENTREGA" => SqlH.ToSequence("DIRECCION_ENTREGA"),
            "ID_GVA14" => SqlH.TraerIdCampo("GVA14", "ID_GVA14", "COD_CLIENT", Get("COD_CLIENTE")),
            "ID_GVA18" => SqlH.TraerId("GVA18", "COD_PROVIN", Get("COD_PROVINCIA")),
            _ => base.FormatValue(field, value)
        };
    }
}
