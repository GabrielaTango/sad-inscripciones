using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>Renglón de pedido (GVA03). Carga defaults de Config/GVA03.xml.</summary>
public class TangoGVA03 : TangoEntity
{
    protected override string TableName => "GVA03";
    protected override string XmlFileName => "GVA03.xml";
    protected override HashSet<string> ExcludeFromInsert => new() { "ID_GVA03" };

    private string TraerCampoGVA21(string campo)
    {
        return $"(SELECT {campo} FROM GVA21 WHERE TALON_PED = {Get("TALON_PED")} AND NRO_PEDIDO = {SqlH.ToString(Get("NRO_PEDIDO"))})";
    }

    protected override string FormatValue(string field, string value)
    {
        return field switch
        {
            "ID_GVA21" => TraerCampoGVA21("ID_GVA21"),
            "ID_STA11" => SqlH.TraerIdCampo("STA11", "ID_STA11", "COD_ARTICU", Get("COD_ARTICU")),
            "ID_MEDIDA_VENTAS" => SqlH.TraerIdCampo("STA11", "ID_MEDIDA_VENTAS", "COD_ARTICU", Get("COD_ARTICU")),
            "ID_MEDIDA_STOCK" => SqlH.TraerIdCampo("STA11", "ID_MEDIDA_STOCK", "COD_ARTICU", Get("COD_ARTICU")),
            "ID_STA22" => SqlH.TraerIdCampoSubConsulta("GVA21", "ID_STA22", "ID_GVA21", TraerCampoGVA21("ID_GVA21")),
            "ID_GVA23" => SqlH.TraerIdCampoSubConsulta("GVA21", "ID_GVA23", "ID_GVA21", TraerCampoGVA21("ID_GVA21")),
            "ID_GVA10" => SqlH.TraerIdCampoSubConsulta("GVA21", "ID_GVA10", "ID_GVA21", TraerCampoGVA21("ID_GVA21")),
            "ID_GVA81" => SqlH.TraerIdCampoSubConsulta("GVA21", "ID_GVA81", "ID_GVA21", TraerCampoGVA21("ID_GVA21")),
            _ => base.FormatValue(field, value)
        };
    }

    /// <summary>Calcula precios netos con/sin IVA (porta GVA03.CalcularPrecio del legacy).</summary>
    public void CalcularPrecio(decimal precio, ItemImpuesto impuesto)
    {
        SetDecimal("PRECIO", precio);
        SetDecimal("PRECIO_LISTA", precio);
        SetDecimal("PRECIO_BONIF", precio);

        decimal precioSinIva, precioConIva;
        if (impuesto.INCLUY_IVA)
        {
            precioSinIva = precio / (1 + impuesto.PORC_IVA);
            precioConIva = precio;
        }
        else
        {
            precioSinIva = precio;
            precioConIva = precio * (1 + impuesto.PORC_IVA);
        }

        var cantPedid = 1m; // inscripciones siempre cantidad 1
        SetDecimal("PRECIO_NETO", precioSinIva);
        SetDecimal("IMPORTE_SIN_IMPUESTOS", precioSinIva * cantPedid);
        SetDecimal("IMPORTE_PROPORCIONADO", precioSinIva * cantPedid);
        SetDecimal("IMPORTE_CON_IMPUESTOS", precioConIva * cantPedid);
    }
}
