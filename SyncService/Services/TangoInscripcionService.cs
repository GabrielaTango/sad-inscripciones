using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Helpers;
using SyncService.Models;

namespace SyncService.Services;

/// <summary>
/// Procesa inscripciones confirmadas del backend y las carga en Tango (ERP).
/// Porta la lógica de ProcesarRecibos (esInscrip=true) del legacy SAD CENTRAL.
/// Usa modelos TangoEntity que cargan defaults desde XML para garantizar todos los campos obligatorios.
/// </summary>
public class TangoInscripcionService
{
    private readonly ILogger<TangoInscripcionService> _logger;
    private readonly IConfiguration _config;
    private readonly TangoClienteService _clienteService;

    private int TalonarioPedido => _config.GetValue("InscripcionSync:TalonarioPedido", 99);
    private int TalonarioFactura => _config.GetValue("InscripcionSync:TalonarioFactura", 8);
    private string TipoAsientoModelo => _config["InscripcionSync:TipoAsientoModelo"] ?? "02";
    private string CotizacionCodMoneda => _config["InscripcionSync:CotizacionCodMoneda"] ?? "DOL";
    private string CotizacionCodTipoCotizacion => _config["InscripcionSync:CotizacionCodTipoCotizacion"] ?? "COTIZACIÓN";

    public TangoInscripcionService(
        ILogger<TangoInscripcionService> logger,
        IConfiguration config,
        TangoClienteService clienteService)
    {
        _logger = logger;
        _config = config;
        _clienteService = clienteService;
    }

    public async Task<bool> ProcesarInscripcionAsync(SqlConnection conn, InscripcionTangoDto insc)
    {
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var inscId = insc.Id.ToString();
            var now = DateTime.Now;

            // 1. Asegurar cliente en Tango (lookup por CUIT, crea si no existe)
            var codClient = await _clienteService.EnsureClienteAsync(conn, tx, DatosClienteTango.FromInscripcion(insc));

            // 1.b Moneda: si es USD se convierte a pesos con la cotización del día.
            //     El comprobante queda en moneda corriente (importe = USD * cotiz) y se
            //     registra la COTIZ como dato. Si es ARS, cotiz=1 y todo queda igual que antes.
            var moneda = (insc.Moneda ?? "ARS").Trim().ToUpperInvariant();
            decimal cotiz = 1m;
            decimal montoComprobante = insc.PrecioFinal;
            if (moneda == "USD")
            {
                var c = await TangoHelpers.TraerCotizacionAsync(conn, tx, CotizacionCodMoneda, CotizacionCodTipoCotizacion);
                if (c is null || c <= 0)
                    throw new InvalidOperationException(
                        $"No hay cotización de {CotizacionCodMoneda} en Tango; la inscripción {insc.Id} (USD) no se puede sincronizar todavía.");
                cotiz = c.Value;
                montoComprobante = Math.Round(insc.PrecioFinal * cotiz, 2);
            }

            // 2. Crear pedido (GVA21)
            var nroPedido = await TangoHelpers.TraerProximoNCompAsync(conn, tx, TalonarioPedido);
            var idAsientoModelo = await TangoHelpers.TraerIdAsientoModeloAsync(conn, tx, TipoAsientoModelo);
            var codArticu = insc.CodArticu ?? "";
            var codVended = await TangoHelpers.TraerCodVendedClienteAsync(conn, tx, codClient) ?? "09";

            var gva21 = new TangoGVA21();
            gva21.Set("FILLER", codClient);
            gva21.Set("COD_CLIENT", codClient);
            gva21.Set("COD_VENDED", codVended);
            gva21.Set("NRO_PEDIDO", nroPedido);
            gva21.SetInt("TALON_PED", TalonarioPedido);
            gva21.SetInt("TALONARIO", TalonarioFactura);
            gva21.SetDate("FECHA_PEDI", now);
            gva21.SetDate("FECHA_INGRESO", now);
            gva21.SetDate("FECHA_ENTR", now);
            gva21.Set("LEYENDA_1", "FACINSCRIP");
            gva21.Set("LEYENDA_2", inscId);
            gva21.SetDecimal("TOTAL_PEDI", montoComprobante);
            gva21.SetDecimal("TOTAL_PEDI_CON_IMPUESTOS", montoComprobante);
            gva21.SetDecimal("COTIZ", cotiz);
            gva21.SetInt("ID_ASIENTO_MODELO_GV", idAsientoModelo);
            gva21.SetDate("FECHA_ULTIMA_MODIFICACION", now);
            await conn.ExecuteAsync(gva21.Insert(), transaction: tx);

            // 3. Crear renglón (GVA03) con cálculo de precios
            var impuesto = await TraerImpuestosAsync(conn, tx, codArticu);

            var gva03 = new TangoGVA03();
            gva03.Set("FILLER", codArticu);
            gva03.Set("COD_ARTICU", codArticu);
            gva03.Set("NRO_PEDIDO", nroPedido);
            gva03.SetInt("TALON_PED", TalonarioPedido);
            gva03.CalcularPrecio(montoComprobante, impuesto);
            await conn.ExecuteAsync(gva03.Insert(), transaction: tx);

            // Actualizar próximo nro pedido
            await conn.ExecuteAsync(TangoHelpers.BuildUpdateProximo(nroPedido, TalonarioPedido), transaction: tx);

            tx.Commit();
            _logger.LogInformation("Inscripción {Id} procesada OK: cliente={CodClient}, pedido={NroPedido}", insc.Id, codClient, nroPedido);
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error procesando inscripción {Id} en Tango", insc.Id);
            return false;
        }
    }

    // ===== Helpers de consulta (específicos de inscripción) =====

    private async Task<ItemImpuesto> TraerImpuestosAsync(SqlConnection conn, SqlTransaction tx, string codArticu)
    {
        // Réplica exacta de TraerImpuestos del legacy (clsProcesos.cs:907)
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT STA11.COD_ARTICU,
                   (ISNULL(IVA.PORCENTAJE,0) / 100) AS PORC_IVA,
                   (ISNULL(IB.PORCENTAJE,0) / 100) AS PORC_IB,
                   (ISNULL(II.PORCENTAJE,0) / 100) AS PORC_II,
                   INCLUY_IVA, INCLUY_IMP
            FROM STA11
            INNER JOIN GVA10 ON GVA10.NRO_DE_LIS = 1
            LEFT OUTER JOIN GVA41 IVA ON IVA.COD_ALICUO = STA11.COD_IVA
            LEFT OUTER JOIN GVA41 IB ON IB.COD_ALICUO = STA11.COD_IB
            LEFT OUTER JOIN GVA41 II ON II.COD_ALICUO = STA11.COD_II
            WHERE STA11.COD_ARTICU = @CodArticu", new { CodArticu = codArticu }, tx);

        if (result == null)
            return new ItemImpuesto { CodArticu = codArticu, PORC_IVA = 0.21m, INCLUY_IVA = true };

        return new ItemImpuesto
        {
            CodArticu = codArticu,
            PORC_IVA = (decimal)result.PORC_IVA,
            PORC_IB = (decimal)result.PORC_IB,
            PORC_II = (decimal)result.PORC_II,
            INCLUY_IVA = (bool)result.INCLUY_IVA,
            INCLUY_IMP = (bool)result.INCLUY_IMP,
        };
    }
}
