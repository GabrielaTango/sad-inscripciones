using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>Factura/Comprobante de venta (GVA12). Carga defaults de Config/GVA12.xml.</summary>
public class TangoGVA12 : TangoEntity
{
    protected override string TableName => "GVA12";
    protected override string XmlFileName => "GVA12.xml";

    protected override HashSet<string> ExcludeFromInsert => new() { "ID_GVA12" };

    protected override string FormatValue(string field, string value)
    {
        return field switch
        {
            "ID_GVA14" => SqlH.TraerIdCampo("GVA14", "ID_GVA14", "COD_CLIENT", Get("COD_CLIENT")),
            "ID_GVA23" => SqlH.TraerIdCampo("GVA23", "ID_GVA23", "COD_VENDED", Get("COD_VENDED")),
            "ID_GVA43" => SqlH.TraerIdCampo("GVA43", "ID_GVA43", "TALONARIO", Get("TALONARIO")),
            "NCOMP_IN_V" => "(SELECT ISNULL(MAX(NCOMP_IN_V),0)+1 FROM GVA12)",
            "ID_GVA12" => "-1",
            _ => base.FormatValue(field, value)
        };
    }

    /// <summary>Genera UPDATE de saldo en GVA14 (porta TOGVA_SALDO del legacy).</summary>
    public string UpdateSaldoCliente()
    {
        var imp = Get("IMPORTE").Replace(",", ".");
        if (!decimal.TryParse(imp, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var _))
            imp = "0.00";
        return $@"UPDATE GVA14 SET SALDO_CC = SALDO_CC - {imp}, SALDO_CC_U = SALDO_CC_U - {imp}
                  WHERE COD_CLIENT = '{Get("COD_CLIENT")}'";
    }
}
