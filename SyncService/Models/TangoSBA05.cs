using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>Detalle de asiento contable (SBA05). Carga defaults de Config/SBA05.xml.</summary>
public class TangoSBA05 : TangoEntity
{
    protected override string TableName => "SBA05";
    protected override string XmlFileName => "SBA05.xml";
    protected override HashSet<string> ExcludeFromInsert => new() { "ID_SBA05" };

    /// <summary>Configura como Haber o Debe (porta SetD_H del legacy).</summary>
    public void ConfigurarDH(string dh)
    {
        Set("D_H", dh);
        if (dh == "D")
        {
            Set("COD_OPERAC", "");
            SetInt("ID_SBA11", -1);
        }
        else
        {
            SetInt("ID_SBA11", -1);
        }

        Set("LEYENDA", "");
        Set("COMENTARIO", "");
        Set("COMENTARIO_EFT", "");
        SetDate("F_CONC_EFT", new DateTime(1800, 1, 1));

    }

    /// <summary>Genera UPDATE de saldo en SBA01 (porta TOSBA01 del legacy).</summary>
    public string UpdateSBA01()
    {
        string signo = Get("D_H") == "H" ? "-" : "+";
        var monto = SqlH.ToDecimal(decimal.Parse(Get("MONTO").Replace(",", "."),
            System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture));

        return $@"UPDATE SBA01 SET
                    SALDO_A_MO = SALDO_A_MO {signo} {monto},
                    SALDO_A_UN = SALDO_A_UN {signo} {monto},
                    SALDO_ACT = SALDO_ACT {signo} {monto}
                  WHERE COD_CTA = {SqlH.ToDecimal(decimal.Parse(Get("COD_CTA").Replace(",", "."),
                    System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture))}";
    }
}
