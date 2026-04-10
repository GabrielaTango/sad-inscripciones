using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>Comprobante de tesorería (SBA04). Carga defaults de Config/SBA04.xml.</summary>
public class TangoSBA04 : TangoEntity
{
    protected override string TableName => "SBA04";
    protected override string XmlFileName => "SBA04.xml";

    protected override HashSet<string> ExcludeFromInsert => new() { "ID_SBA04" };

    /// <summary>Genera INSERT en ASIENTO_COMPROBANTE_SB (porta TOASIENTO_COMPROBANTE_SB del legacy).</summary>
    public string InsertAsientoComprobante()
    {
        return $@"
            INSERT INTO ASIENTO_COMPROBANTE_SB (
                ID_ASIENTO_COMPROBANTE_SB, N_INTERNO,
                ASIENTO_ANULACION, CONTABILIZADO,
                USUARIO_CONTABILIZACION, FECHA_CONTABILIZACION, TERMINAL_CONTABILIZACION,
                TRANSFERIDO_CN
            ) VALUES (
                NEXT VALUE FOR SEQUENCE_ASIENTO_COMPROBANTE_SB,
                (SELECT N_INTERNO FROM SBA04 WHERE COD_COMP = '{Get("COD_COMP")}' AND N_COMP = '{Get("N_COMP")}'),
                'N', 'N', NULL, NULL, NULL, 'N'
            )";
    }
}
