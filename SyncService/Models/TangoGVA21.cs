using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>Pedido de venta (GVA21). Carga defaults de Config/GVA21.xml.</summary>
public class TangoGVA21 : TangoEntity
{
    protected override string TableName => "GVA21";
    protected override string XmlFileName => "GVA21.xml";

    protected override HashSet<string> ExcludeFromInsert => new() { "ID_GVA21" };

    protected override string FormatValue(string field, string value)
    {
        return field switch
        {
            "ID_GVA01" => SqlH.TraerIdCampo("GVA01", "ID_GVA01", "COND_VTA", Get("COND_VTA")),
            "ID_GVA10" => SqlH.TraerIdCampo("GVA10", "ID_GVA10", "NRO_DE_LIS", Get("N_LISTA")),
            "ID_GVA14" => SqlH.TraerIdCampo("GVA14", "ID_GVA14", "COD_CLIENT", Get("COD_CLIENT")),
            "ID_GVA23" => SqlH.TraerIdCampo("GVA23", "ID_GVA23", "COD_VENDED", Get("COD_VENDED")),
            "ID_GVA24" => SqlH.TraerIdCampo("GVA24", "ID_GVA24", "COD_TRANSP", Get("COD_TRANSP")),
            "ID_GVA43_TALON_PED" => SqlH.TraerIdCampo("GVA43", "ID_GVA43", "TALONARIO", Get("TALON_PED")),
            "ID_GVA43_TALONARIO_FACTURA" => SqlH.TraerIdCampo("GVA43", "ID_GVA43", "TALONARIO", Get("TALONARIO")),
            "ID_GVA81" => SqlH.TraerIdCampo("GVA81", "ID_GVA81", "COD_CLASIF", Get("COD_CLASIF")),
            "ID_STA22" => SqlH.TraerIdCampo("STA22", "ID_STA22", "COD_SUCURS", Get("COD_SUCURS")),
            _ => base.FormatValue(field, value)
        };
    }
}
