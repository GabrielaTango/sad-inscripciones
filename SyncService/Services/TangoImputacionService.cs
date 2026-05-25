using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Helpers;
using SyncService.Models;

namespace SyncService.Services;

/// <summary>
/// Porta el método legacy ImputarInscripciones(): busca pares factura↔recibo
/// pendientes de imputar (GVA12 FACINSCRIP/REC con LEYENDA_2 compartido) y
/// genera las imputaciones GVA07 correspondientes.
/// </summary>
public class TangoImputacionService
{
    private readonly ILogger<TangoImputacionService> _logger;
    private readonly IConfiguration _config;

    public TangoImputacionService(ILogger<TangoImputacionService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<int> EjecutarPendientesAsync()
    {
        var connStr = _config["SqlServerConnection"];
        if (string.IsNullOrEmpty(connStr))
        {
            _logger.LogWarning("SqlServerConnection no configurada, saltando imputaciones");
            return 0;
        }

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var pendientes = (await ConsultarPendientesAsync(conn, tx)).ToList();
            if (pendientes.Count == 0)
            {
                tx.Rollback();
                _logger.LogDebug("Sin imputaciones pendientes");
                return 0;
            }

            _logger.LogInformation("Procesando {Count} imputaciones pendientes", pendientes.Count);

            foreach (var row in pendientes)
            {
                var gva07 = new TangoGVA07();
                gva07.Set("FILLER", row.Inscrip ?? string.Empty);
                gva07.SetDate("FECHA_VTO", row.FechaVto);
                gva07.SetDate("F_COMP_CAN", DateTime.Now.Date);
                gva07.Set("T_COMP", row.TComp ?? string.Empty);
                gva07.Set("N_COMP", row.NComp ?? string.Empty);
                gva07.SetDecimal("IMPORTE_VT", row.Importe);
                gva07.SetDecimal("IMP_VT_UNI", row.Importe);
                gva07.Set("T_COMP_CAN", row.TCompCan ?? string.Empty);
                gva07.Set("N_COMP_CAN", row.NCompCan ?? string.Empty);
                gva07.SetDecimal("IMPORT_CAN", row.ImportCan);
                gva07.SetInt("ID_GVA12_CAN", row.IdGva12Can);
                await conn.ExecuteAsync(gva07.Insert(), transaction: tx);

                await conn.ExecuteAsync(
                    @"UPDATE GVA12 SET ESTADO = 'CAN', ESTADO_UNI = 'CAN'
                      WHERE ID_GVA12 = @Id",
                    new { Id = row.IdGva12Fac }, tx);

                await conn.ExecuteAsync(
                    @"UPDATE GVA12 SET ESTADO = 'IMP', ESTADO_UNI = 'IMP'
                      WHERE ID_GVA12 = @Id",
                    new { Id = row.IdGva12Can }, tx);

                await conn.ExecuteAsync(
                    @"UPDATE GVA46 SET ESTADO_VTO = 'PAG', ESTADO_UNI = 'PAG'
                      WHERE T_COMP = @TComp AND N_COMP = @NComp AND FECHA_VTO = @FechaVto",
                    new { TComp = row.TComp, NComp = row.NComp, FechaVto = row.FechaVto }, tx);
            }

            tx.Commit();
            _logger.LogInformation("Imputaciones procesadas OK: {Count}", pendientes.Count);
            return pendientes.Count;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error ejecutando imputaciones pendientes");
            throw;
        }
    }

    private static async Task<IEnumerable<ImputacionRow>> ConsultarPendientesAsync(SqlConnection conn, SqlTransaction tx)
    {
        // Query del legacy: matchea facturas FACINSCRIP con recibos REC por LEYENDA_2 (= inscripcionId).
        // Nota: el legacy original filtra FAC.ESTADO = 'PEN'; lo extendemos a CTA para cubrir el
        // estado actual del TangoInscripcionService hasta unificar la convención.
        const string sql = @"
            SELECT FAC.LEYENDA_2 AS Inscrip,
                   FAC.T_COMP AS TComp,
                   FAC.N_COMP AS NComp,
                   FAC.IMPORTE AS Importe,
                   REC.T_COMP AS TCompCan,
                   REC.N_COMP AS NCompCan,
                   REC.IMPORTE AS ImportCan,
                   GVA46.FECHA_VTO AS FechaVto,
                   REC.ID_GVA12 AS IdGva12Can,
                   FAC.ID_GVA12 AS IdGva12Fac
            FROM GVA12 FAC
            INNER JOIN GVA46 ON GVA46.T_COMP = FAC.T_COMP AND GVA46.N_COMP = FAC.N_COMP
            INNER JOIN GVA12 REC ON FAC.LEYENDA_2 = REC.LEYENDA_2 AND REC.T_COMP = 'REC' AND FAC.COD_CLIENT = REC.COD_CLIENT
            WHERE FAC.LEYENDA_1 = 'FACINSCRIP'
              AND FAC.LEYENDA_2 <> ''
              AND FAC.ESTADO IN ('CTA', 'PEN')
              AND REC.ESTADO = 'CTA'";

        return await conn.QueryAsync<ImputacionRow>(sql, transaction: tx);
    }

    private sealed class ImputacionRow
    {
        public string? Inscrip { get; set; }
        public string? TComp { get; set; }
        public string? NComp { get; set; }
        public decimal Importe { get; set; }
        public string? TCompCan { get; set; }
        public string? NCompCan { get; set; }
        public decimal ImportCan { get; set; }
        public DateTime FechaVto { get; set; }
        public int IdGva12Can { get; set; }
        public int IdGva12Fac { get; set; }
    }
}
