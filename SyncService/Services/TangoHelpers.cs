using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Helpers;

namespace SyncService.Services;

internal static class TangoHelpers
{
    public static async Task<string> TraerProximoNCompAsync(SqlConnection conn, SqlTransaction tx, int talonario)
    {
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT TIPO, SUCURSAL, PROXIMO FROM GVA43 WHERE TALONARIO = @Tal", new { Tal = talonario }, tx);
        if (row == null) throw new InvalidOperationException($"Talonario {talonario} no encontrado en GVA43");

        string tipo = ((string)row.TIPO).PadLeft(1, ' ');
        string sucursal = (string)row.SUCURSAL;
        string proximoDecrypted = SqlH.DocNumberDecrypt((string)row.PROXIMO).PadLeft(8, '0');
        return tipo + sucursal + proximoDecrypted;
    }

    public static string BuildUpdateProximo(string nComp, int talonario)
    {
        long prox = long.Parse(nComp.Substring(6, 8));
        string encrypted = SqlH.DocNumberEncrypt((prox + 1).ToString().PadLeft(8, '0'));
        return $"UPDATE GVA43 SET PROXIMO = '{encrypted}' WHERE TALONARIO = {talonario}";
    }

    public static async Task<int> TraerProximoNInternoAsync(SqlConnection conn, SqlTransaction tx)
    {
        var max = await conn.QueryFirstOrDefaultAsync<decimal?>(
            "SELECT CAST(MAX(N_INTERNO) AS DECIMAL) FROM SBA04", transaction: tx);
        return (int)(max ?? 0) + 1;
    }

    public static async Task<int> TraerIdAsientoModeloAsync(SqlConnection conn, SqlTransaction tx, string tipo)
    {
        return await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT ID_ASIENTO_MODELO_GV FROM ASIENTO_MODELO_GV WHERE COD_ASIENTO_MODELO_GV = @Tipo",
            new { Tipo = tipo }, tx) ?? -1;
    }

    public static async Task<string?> TraerCodVendedClienteAsync(SqlConnection conn, SqlTransaction tx, string codClient)
    {
        var cod = await conn.QueryFirstOrDefaultAsync<string?>(
            "SELECT COD_VENDED FROM GVA14 WHERE COD_CLIENT = @CodClient",
            new { CodClient = codClient }, tx);
        return string.IsNullOrWhiteSpace(cod) ? null : cod.Trim();
    }

    // Cuenta del vendedor del cliente leída de CAMPOS_ADICIONALES de GVA23.
    // Hay tres "buckets" según el origen del cobro:
    //   CA_1118_CTA_CUOTAS       → recibos de cuenta corriente (cuotas societarias).
    //   CA_1118_CTA_OTRA         → recibos de eventos (inscripciones, congresos).
    //   CA_1118_CTA_MERCADO_PAGO → conservado por si algún flujo legacy lo usa.
    public static Task<decimal> TraerCuentaCuotasAsync(SqlConnection conn, SqlTransaction tx, string codClient)
        => TraerCuentaCampoAdicionalAsync(conn, tx, codClient, "CA_1118_CTA_CUOTAS");

    public static Task<decimal> TraerCuentaOtraAsync(SqlConnection conn, SqlTransaction tx, string codClient)
        => TraerCuentaCampoAdicionalAsync(conn, tx, codClient, "CA_1118_CTA_OTRA");

    private static async Task<decimal> TraerCuentaCampoAdicionalAsync(
        SqlConnection conn, SqlTransaction tx, string codClient, string campo)
    {
        var cta = await conn.QueryFirstOrDefaultAsync<decimal?>(
            $@"SELECT CAST(t2.CAMPOS_ADICIONALES.value('(/CAMPOS_ADICIONALES/{campo})[1]', 'varchar(10)') AS DECIMAL)
               FROM GVA14 t1 INNER JOIN GVA23 t2 ON t1.COD_VENDED = t2.COD_VENDED
               WHERE t1.COD_CLIENT = @CodClient AND t1.COD_VENDED <> '90'",
            new { CodClient = codClient }, tx);
        return cta ?? 0;
    }
}
