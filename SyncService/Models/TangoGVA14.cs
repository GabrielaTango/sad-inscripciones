using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>Cliente (GVA14). Carga defaults de Config/GVA14.xml.</summary>
public class TangoGVA14 : TangoEntity
{
    protected override string TableName => "GVA14";
    protected override string XmlFileName => "GVA14.xml";

    protected override string FormatValue(string field, string value)
    {
        return field switch
        {
            "ID_GVA14" => "NEXT VALUE FOR SEQUENCE_GVA14",
            "ID_GVA18" => SqlH.TraerId("GVA18", "COD_PROVIN", Get("COD_PROVIN")),
            "ID_GVA05" => SqlH.TraerId("GVA05", "COD_ZONA", Get("COD_ZONA")),
            "ID_GVA01" => SqlH.TraerIdCampo("GVA01", "ID_GVA01", "COND_VTA", Get("COND_VTA")),
            "ID_GVA10" => SqlH.TraerIdCampo("GVA10", "ID_GVA10", "NRO_DE_LIS", Get("NRO_LISTA")),
            "ID_GVA23" => SqlH.TraerIdCampo("GVA23", "ID_GVA23", "COD_VENDED", Get("COD_VENDED")),
            "ID_GVA24" => SqlH.TraerIdCampo("GVA24", "ID_GVA24", "COD_TRANSP", Get("COD_TRANSP")),
            "ID_TIPO_DOCUMENTO_GV" => $"(SELECT TOP 1 ID_TIPO_DOCUMENTO_GV FROM TIPO_DOCUMENTO_GV WHERE TIPO_DOCUMENTO = {Get("TIPO_DOC")})",
            "ID_SUCURSAL" => "1",
            _ => base.FormatValue(field, value)
        };
    }
}
