namespace SyncService.Models;

/// <summary>Imputación recibo ↔ factura en GVA07. Carga defaults de Config/GVA07.xml.</summary>
public class TangoGVA07 : TangoEntity
{
    protected override string TableName => "GVA07";
    protected override string XmlFileName => "GVA07.xml";

    protected override HashSet<string> ExcludeFromInsert => new() { "ID_GVA07" };
}
