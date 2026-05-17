using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Models;

namespace SyncService.Services;

/// <summary>
/// Procesa pagos confirmados (inscripción o seña) y genera el recibo
/// correspondiente en Tango. No emite pedido ni factura — eso lo hace
/// TangoInscripcionService cuando la inscripción queda Confirmada.
/// </summary>
public class TangoPagoService
{
    private readonly ILogger<TangoPagoService> _logger;
    private readonly IConfiguration _config;
    private readonly TangoClienteService _clienteService;

    private int TalonarioRecibo => _config.GetValue("InscripcionSync:TalonarioRecibo", 16);
    private decimal CuentaDebeMercadoPago => _config.GetValue("InscripcionSync:CuentaDebeMercadoPago", 125M);
    private int IdSBA02 => _config.GetValue("InscripcionSync:IdSBA02", 7);
    private string TipoAsientoModelo => _config["InscripcionSync:TipoAsientoModelo"] ?? "02";

    public TangoPagoService(
        ILogger<TangoPagoService> logger,
        IConfiguration config,
        TangoClienteService clienteService)
    {
        _logger = logger;
        _config = config;
        _clienteService = clienteService;
    }

    public async Task<bool> ProcesarPagoAsync(SqlConnection conn, PagoTangoDto pago)
    {
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var inscId = pago.InscripcionId.ToString();
            var now = DateTime.Now;

            // 1. Asegurar cliente en Tango
            var codClient = await _clienteService.EnsureClienteAsync(conn, tx, DatosClienteTango.FromPago(pago));

            // 2. Numero de comprobante del recibo
            var nComp = await TangoHelpers.TraerProximoNCompAsync(conn, tx, TalonarioRecibo);
            var codVended = await TangoHelpers.TraerCodVendedClienteAsync(conn, tx, codClient) ?? "09";

            // 3. Crear cabecera del recibo en GVA12 (T_COMP='REC', ESTADO='CTA')
            // Necesario para que la imputación posterior (GVA07) pueda referenciar ID_GVA12_CAN.
            var gva12Rec = new TangoGVA12();
            gva12Rec.Set("FILLER", inscId);
            gva12Rec.Set("COD_CLIENT", codClient);
            gva12Rec.Set("COD_VENDED", codVended);
            gva12Rec.Set("N_COMP", nComp);
            gva12Rec.SetInt("TALONARIO", TalonarioRecibo);
            gva12Rec.SetDate("FECHA_EMIS", now);
            gva12Rec.SetDate("FECHA_INGRESO", now);
            gva12Rec.SetDecimal("IMPORTE", pago.Monto);
            gva12Rec.SetDecimal("UNIDADES", pago.Monto);
            gva12Rec.Set("ESTADO", "CTA");
            gva12Rec.Set("ESTADO_UNI", "CTA");
            gva12Rec.Set("LEYENDA_1", "RECINSCRIP");
            gva12Rec.Set("LEYENDA_2", inscId);
            await conn.ExecuteAsync(gva12Rec.Insert(), transaction: tx);

            // 4. Crear comprobante de tesorería (SBA04)
            // FILLER lleva el inscripcion id para trazabilidad (coincide con factura/pedido);
            // el pago id queda en el CONCEPTO
            var nInterno = await TangoHelpers.TraerProximoNInternoAsync(conn, tx);

            var sba04 = new TangoSBA04();
            sba04.Set("FILLER", inscId);
            sba04.Set("N_COMP", nComp);
            sba04.Set("COD_COMP", "REC");
            sba04.SetInt("N_INTERNO", nInterno);
            sba04.SetDate("FECHA", now);
            sba04.SetDate("FECHA_EMIS", now);
            sba04.SetDate("FECHA_ING", now);
            sba04.Set("COD_GVA14", codClient);
            sba04.SetDecimal("TOTAL_IMPORTE_CTE", pago.Monto);
            sba04.SetDecimal("TOTAL_IMPORTE_EXT", pago.Monto);
            sba04.Set("CONCEPTO", ConstruirConcepto(pago));
            sba04.SetInt("ID_SBA02", IdSBA02);
            sba04.SetDate("FECHA_ULTIMA_MODIFICACION", new DateTime(1, 1, 1));
            await conn.ExecuteAsync(sba04.Insert(), transaction: tx);

            // 4. Asiento contable del comprobante
            await conn.ExecuteAsync(sba04.InsertAsientoComprobante(), transaction: tx);

            // 5. SBA05 Haber: cuenta OTRA del vendedor del cliente (eventos).
            var cuentaOtra = await TangoHelpers.TraerCuentaOtraAsync(conn, tx, codClient);
            var sba05h = new TangoSBA05();
            sba05h.ConfigurarDH("H");
            sba05h.Set("FILLER", inscId);
            sba05h.Set("N_COMP", nComp);
            sba05h.Set("COD_COMP", "REC");
            sba05h.SetDecimal("COD_CTA", cuentaOtra);
            sba05h.SetDecimal("MONTO", pago.Monto);
            sba05h.SetDecimal("CANT_MONE", pago.Monto);
            sba05h.SetDecimal("UNIDADES", pago.Monto);
            sba05h.SetDate("FECHA", now);
            sba05h.Set("COD_GVA14", codClient);
            sba05h.SetInt("ID_SBA02", IdSBA02);
            sba05h.SetDate("F_CONC_EFT", new DateTime(1, 1, 1));
            await conn.ExecuteAsync(sba05h.Insert(), transaction: tx);
            await conn.ExecuteAsync(sba05h.UpdateSBA01(), transaction: tx);

            // 6. SBA05 Debe: cuenta caja Mercado Pago (config).
            var sba05d = new TangoSBA05();
            sba05d.ConfigurarDH("D");
            sba05d.Set("FILLER", inscId);
            sba05d.Set("N_COMP", nComp);
            sba05d.Set("COD_COMP", "REC");
            sba05d.SetDecimal("COD_CTA", CuentaDebeMercadoPago);
            sba05d.SetDecimal("MONTO", pago.Monto);
            sba05d.SetDecimal("CANT_MONE", pago.Monto);
            sba05d.SetDecimal("UNIDADES", pago.Monto);
            sba05d.SetDate("FECHA", now);
            sba05d.Set("COD_GVA14", codClient);
            sba05d.SetInt("ID_SBA02", IdSBA02);
            sba05d.SetDate("F_CONC_EFT", new DateTime(1, 1, 1));
            await conn.ExecuteAsync(sba05d.Insert(), transaction: tx);
            await conn.ExecuteAsync(sba05d.UpdateSBA01(), transaction: tx);

            // 7. Avanzar numeración del talonario
            await conn.ExecuteAsync(TangoHelpers.BuildUpdateProximo(nComp, TalonarioRecibo), transaction: tx);

            // 8. Actualizar INCREMENTAL_VALUE de SBA04
            await conn.ExecuteAsync(
                "UPDATE INCREMENTAL_VALUE SET ULTIMOVALOR = (SELECT MAX(N_INTERNO) FROM SBA04) WHERE TABLA = 'SBA04' AND CAMPO = 'N_INTERNO'",
                transaction: tx);

            tx.Commit();
            _logger.LogInformation("Pago {Id} procesado OK: cliente={CodClient}, recibo={NComp}, monto={Monto}",
                pago.Id, codClient, nComp, pago.Monto);
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error procesando pago {Id} en Tango (inscripcion={InscId})", pago.Id, pago.InscripcionId);
            return false;
        }
    }

    private static string ConstruirConcepto(PagoTangoDto pago)
    {
        var medio = string.IsNullOrWhiteSpace(pago.MedioPago) ? "pago" : pago.MedioPago;
        return $"{medio} insc {pago.InscripcionId}";
    }
}
