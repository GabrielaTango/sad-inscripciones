using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Models;

namespace SyncService.Services;

/// <summary>
/// Carga el recibo REC en Tango por un pago de resumen de cuenta aprobado en MercadoPago
/// e intenta imputarlo contra los comprobantes seleccionados por el socio.
///
/// Flujo (una transacción):
///   1. Lookup COD_CLIENT por CUIT en GVA14 (no crea — el socio debe existir porque ya tiene cuenta corriente).
///   2. Genera REC (GVA12 + SBA04 + SBA05 D/H) por el monto total del pago.
///   3. Por cada comprobante seleccionado (ordenado por FechaVto asc): imputa min(saldoActualEnTango, montoRestante).
///      Si el saldo actual es menor al saldo al momento del pago, imputa parcial y deja el sobrante como crédito a favor.
///   4. Ajusta ESTADO del REC a 'IMP' si se imputó algo, del comprobante a 'CAN' si queda sin saldo pendiente,
///      y de GVA46 a 'PAG' por cada vencimiento cancelado.
///
/// Devuelve (ok, montoImputado). Un monto imputado menor al monto del pago indica que parte quedó como
/// saldo a favor en cuenta corriente (socios u operaciones contables lo resuelven manualmente en Tango).
/// </summary>
public class TangoResumenCuentaService
{
    private readonly ILogger<TangoResumenCuentaService> _logger;
    private readonly IConfiguration _config;

    private int TalonarioRecibo => _config.GetValue("InscripcionSync:TalonarioRecibo", 16);
    // Cuenta debe por medio de pago. La haber sale del vendedor del cliente
    // (CA_1118_CTA_CUOTAS para CC, CA_1118_CTA_OTRA para eventos).
    private decimal CuentaDebeMercadoPago => _config.GetValue("InscripcionSync:CuentaDebeMercadoPago", 125M);
    private decimal CuentaDebePagoFacil => _config.GetValue("InscripcionSync:CuentaDebePagoFacil", 49M);
    // Solo se usa como fallback en cobros presenciales sin CtaTesoreria (caso raro).
    private decimal CuentaHaberPresencialFallback => _config.GetValue("InscripcionSync:CuentaHaber", 18M);
    private int IdSBA02 => _config.GetValue("InscripcionSync:IdSBA02", 7);
    private string TipoAsientoModelo => _config["InscripcionSync:TipoAsientoModelo"] ?? "02";

    public TangoResumenCuentaService(ILogger<TangoResumenCuentaService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<(bool ok, decimal montoImputado)> ProcesarPagoAsync(SqlConnection conn, PagoCuentaCorrienteTangoDto pago)
    {
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var cuit = pago.Cuit?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(cuit))
            {
                _logger.LogError("Pago CC {Id} sin CUIT, no se puede procesar", pago.Id);
                tx.Rollback();
                return (false, 0m);
            }

            var comprobantes = ParseComprobantes(pago.Comprobantes);
            var pagoACuenta = comprobantes.Count == 0;

            // 1. Lookup cliente (no crear — el socio debe existir por tener CC)
            var codClient = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 COD_CLIENT FROM GVA14 WHERE CUIT = @Cuit",
                new { Cuit = cuit }, tx);

            if (string.IsNullOrEmpty(codClient))
            {
                _logger.LogError("Cliente con CUIT {Cuit} no existe en Tango para pago CC {Id}", cuit, pago.Id);
                tx.Rollback();
                return (false, 0m);
            }
            codClient = codClient.Trim();

            var now = DateTime.Now;
            var extRef = pago.ExternalReference ?? string.Empty;

            // Resolución de cuentas para un recibo de cuenta corriente.
            //   Haber = cuenta CUOTAS del vendedor del cliente (CA_1118_CTA_CUOTAS).
            //   Debe  = cuenta caja según medio:
            //     - PagoFacil → CuentaDebePagoFacil (92 por config).
            //     - MercadoPago (default socio) → CuentaDebeMercadoPago (125).
            //     - Presencial (cobro de capítulo) → CtaTesoreria del cobrador,
            //       que el backend ya resolvió a CtaCaja (Contado) o CtaTransferencia
            //       (Transferencia) según el medio elegido.
            var esPagoFacil = string.Equals(pago.MedioPago, "PagoFacil", StringComparison.OrdinalIgnoreCase);
            var esPresencial = !esPagoFacil && pago.CtaTesoreria.HasValue && pago.CtaTesoreria.Value > 0;

            var cuentaCuotas = await TangoHelpers.TraerCuentaCuotasAsync(conn, tx, codClient);

            decimal cuentaHaberFinal;
            decimal cuentaDebeFinal;
            if (esPresencial)
            {
                cuentaHaberFinal = cuentaCuotas == 0 ? CuentaHaberPresencialFallback : cuentaCuotas;
                cuentaDebeFinal = pago.CtaTesoreria!.Value;
            }
            else if (esPagoFacil)
            {
                cuentaHaberFinal = cuentaCuotas;
                cuentaDebeFinal = CuentaDebePagoFacil;
            }
            else
            {
                cuentaHaberFinal = cuentaCuotas;
                cuentaDebeFinal = CuentaDebeMercadoPago;
            }

            // 2. Crear recibo REC
            var nCompRec = await TangoHelpers.TraerProximoNCompAsync(conn, tx, TalonarioRecibo);
            var codVended = await TangoHelpers.TraerCodVendedClienteAsync(conn, tx, codClient) ?? "09";

            var gva12Rec = new TangoGVA12();
            //gva12Rec.Set("FILLER", extRef);
            gva12Rec.Set("COD_CLIENT", codClient);
            gva12Rec.Set("COD_VENDED", codVended);
            gva12Rec.Set("N_COMP", nCompRec);
            gva12Rec.SetInt("TALONARIO", TalonarioRecibo);
            gva12Rec.SetDate("FECHA_EMIS", now);
            gva12Rec.SetDate("FECHA_INGRESO", now);
            gva12Rec.SetDecimal("IMPORTE", pago.Monto);
            gva12Rec.SetDecimal("UNIDADES", pago.Monto);
            gva12Rec.Set("ESTADO", "CTA");
            gva12Rec.Set("ESTADO_UNI", "CTA");
            gva12Rec.Set("LEYENDA_1", "RECCC");
            gva12Rec.Set("LEYENDA_2", Truncar(extRef, 40));
            await conn.ExecuteAsync(gva12Rec.Insert(), transaction: tx);

            // 4. Actualizar saldo del cliente (debe del cliente por el pedido emitido)
            await conn.ExecuteAsync(gva12Rec.UpdateSaldoCliente(), transaction: tx);

            // SBA04
            var nInterno = await TangoHelpers.TraerProximoNInternoAsync(conn, tx);
            var sba04 = new TangoSBA04();
            //sba04.Set("FILLER", extRef);
            sba04.Set("N_COMP", nCompRec);
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

            sba04.Set("C_COMP_ORI", "");
            sba04.Set("N_COMP_ORI", "");
            
            sba04.Set("TERM_ULTIMA_MODIFICACION", "");
            sba04.Set("HORA_ULTIMA_MODIFICACION", "");
            sba04.Set("USUA_ULTIMA_MODIFICACION", "");


            await conn.ExecuteAsync(sba04.Insert(), transaction: tx);
            await conn.ExecuteAsync(sba04.InsertAsientoComprobante(), transaction: tx);

            // SBA05 Haber
            var sba05h = new TangoSBA05();
            sba05h.ConfigurarDH("H");
            //sba05h.Set("FILLER", extRef);
            sba05h.Set("N_COMP", nCompRec);
            sba05h.Set("COD_COMP", "REC");
            sba05h.SetDecimal("COD_CTA", cuentaHaberFinal);
            sba05h.SetDecimal("MONTO", pago.Monto);
            sba05h.SetDecimal("CANT_MONE", pago.Monto);
            sba05h.SetDecimal("UNIDADES", pago.Monto);
            sba05h.SetDate("FECHA", now);
            sba05h.Set("COD_GVA14", codClient);
            sba05h.SetInt("ID_SBA02", IdSBA02);
            await conn.ExecuteAsync(sba05h.Insert(), transaction: tx);
            await conn.ExecuteAsync(sba05h.UpdateSBA01(), transaction: tx);

            // SBA05 Debe
            var sba05d = new TangoSBA05();
            sba05d.ConfigurarDH("D");
            //sba05d.Set("FILLER", extRef);
            sba05d.Set("N_COMP", nCompRec);
            sba05d.Set("COD_COMP", "REC");
            sba05d.SetDecimal("COD_CTA", cuentaDebeFinal);
            sba05d.SetDecimal("MONTO", pago.Monto);
            sba05d.Set("COD_OPERAC", "");
            sba05d.SetDecimal("CANT_MONE", pago.Monto);
            sba05d.SetDecimal("UNIDADES", pago.Monto);
            sba05d.SetDate("FECHA", now);
            sba05d.Set("COD_GVA14", codClient);
            sba05d.SetInt("ID_SBA02", IdSBA02);
            await conn.ExecuteAsync(sba05d.Insert(), transaction: tx);
            await conn.ExecuteAsync(sba05d.UpdateSBA01(), transaction: tx);

            // Avanzar numeración + incremental
            await conn.ExecuteAsync(TangoHelpers.BuildUpdateProximo(nCompRec, TalonarioRecibo), transaction: tx);
            await conn.ExecuteAsync(
                "UPDATE INCREMENTAL_VALUE SET ULTIMOVALOR = (SELECT MAX(N_INTERNO) FROM SBA04) WHERE TABLA = 'SBA04' AND CAMPO = 'N_INTERNO'",
                transaction: tx);

            // 3. Imputaciones (saltear si es pago a cuenta)
            var montoImputado = 0m;
            int? idGva12Rec = null;

            if (pagoACuenta)
            {
                _logger.LogInformation(
                    "Pago CC {Id}: pago a cuenta sin imputaciones (queda saldo a favor en Tango)",
                    pago.Id);
            }
            else
            {
                idGva12Rec = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT ID_GVA12 FROM GVA12 WHERE T_COMP = 'REC' AND N_COMP = @NComp",
                    new { NComp = nCompRec }, tx);

                if (idGva12Rec is null or <= 0)
                    throw new InvalidOperationException($"No se pudo recuperar ID_GVA12 del recibo {nCompRec}");

                var montoRestante = pago.Monto;

                foreach (var c in comprobantes.OrderBy(x => x.FechaVto))
                {
                if (montoRestante <= 0) break;

                var saldoInfo = await conn.QueryFirstOrDefaultAsync<SaldoComprobanteRow>(@"
                    SELECT GVA46.IMPORTE_VT + ISNULL(SUM(ISNULL(IMPU.IMPORT_CAN,0) * CASE WHEN GVA15.TIPO_COMP = 'D' THEN 1 ELSE -1 END), 0) AS Saldo,
                           MAX(GVA12.ID_GVA12) AS IdGva12
                    FROM GVA46
                    LEFT OUTER JOIN GVA07 IMPU ON IMPU.T_COMP = GVA46.T_COMP AND IMPU.N_COMP = GVA46.N_COMP AND IMPU.FECHA_VTO = GVA46.FECHA_VTO
                    LEFT OUTER JOIN GVA15 ON GVA15.IDENT_COMP = IMPU.T_COMP_CAN
                    LEFT OUTER JOIN GVA12 ON GVA12.T_COMP = GVA46.T_COMP AND GVA12.N_COMP = GVA46.N_COMP
                    WHERE GVA46.T_COMP = @TComp AND GVA46.N_COMP = @NComp AND GVA46.FECHA_VTO = @FechaVto
                    GROUP BY GVA46.T_COMP, GVA46.N_COMP, GVA46.FECHA_VTO, GVA46.IMPORTE_VT",
                    new { c.TComp, c.NComp, c.FechaVto }, tx);

                if (saldoInfo == null || saldoInfo.Saldo <= 0)
                {
                    _logger.LogInformation(
                        "Pago CC {Id}: comprobante {TComp}/{NComp} venc {Fecha:yyyy-MM-dd} ya cancelado en Tango, skip",
                        pago.Id, c.TComp, c.NComp, c.FechaVto);
                    continue;
                }

                var imputable = Math.Min(saldoInfo.Saldo, montoRestante);
                if (imputable <= 0) continue;

                var gva07 = new TangoGVA07();
                //gva07.Set("FILLER", extRef);
                gva07.SetDate("FECHA_VTO", c.FechaVto);
                gva07.SetDate("F_COMP_CAN", now.Date);
                gva07.Set("T_COMP", c.TComp);
                gva07.Set("N_COMP", c.NComp);
                gva07.SetDecimal("IMPORTE_VT", saldoInfo.Saldo);
                gva07.SetDecimal("IMP_VT_UNI", saldoInfo.Saldo);
                gva07.Set("T_COMP_CAN", "REC");
                gva07.Set("N_COMP_CAN", nCompRec);
                gva07.SetDecimal("IMPORT_CAN", imputable);
                gva07.SetDecimal("IMP_CAN_UN", imputable);
                gva07.SetInt("ID_GVA12_CAN", idGva12Rec.Value);
                await conn.ExecuteAsync(gva07.Insert(), transaction: tx);

                // Si se canceló el vencimiento completo, marcarlo PAG
                if (imputable >= saldoInfo.Saldo)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE GVA46 SET ESTADO_VTO = 'PAG', ESTADO_UNI = 'PAG'
                        WHERE T_COMP = @TComp AND N_COMP = @NComp AND FECHA_VTO = @FechaVto",
                        new { c.TComp, c.NComp, c.FechaVto }, tx);

                    // Si ya no queda saldo en ningún vencimiento del comprobante, marcar GVA12 como CAN
                    var pendientes = await conn.QueryFirstOrDefaultAsync<int>(@"
                        SELECT COUNT(*) FROM GVA46
                        WHERE T_COMP = @TComp AND N_COMP = @NComp AND ESTADO_VTO <> 'PAG'",
                        new { c.TComp, c.NComp }, tx);

                    if (pendientes == 0 && saldoInfo.IdGva12 > 0)
                    {
                        await conn.ExecuteAsync(@"
                            UPDATE GVA12 SET ESTADO = 'CAN', ESTADO_UNI = 'CAN'
                            WHERE ID_GVA12 = @Id",
                            new { Id = saldoInfo.IdGva12 }, tx);
                    }
                }

                if (imputable < saldoInfo.Saldo)
                {
                    _logger.LogWarning(
                        "Pago CC {Id}: imputación parcial en {TComp}/{NComp} venc {Fecha:yyyy-MM-dd}: saldo={Saldo}, imputado={Imp}",
                        pago.Id, c.TComp, c.NComp, c.FechaVto, saldoInfo.Saldo, imputable);
                }

                    montoRestante -= imputable;
                    montoImputado += imputable;
                }

                // Marcar recibo como imputado si corresponde
                if (montoImputado > 0)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE GVA12 SET ESTADO = 'IMP', ESTADO_UNI = 'IMP'
                        WHERE ID_GVA12 = @Id",
                        new { Id = idGva12Rec!.Value }, tx);
                }

                if (montoImputado < pago.Monto)
                {
                    _logger.LogWarning(
                        "Pago CC {Id}: imputado {Imp} de {Total} (queda {Resto} como saldo a favor en Tango)",
                        pago.Id, montoImputado, pago.Monto, pago.Monto - montoImputado);
                }
            }

            tx.Commit();
            _logger.LogInformation(
                "Pago CC {Id} procesado OK: cliente={CodClient}, recibo={NComp}, monto={Monto}, imputado={Imp}",
                pago.Id, codClient, nCompRec, pago.Monto, montoImputado);
            return (true, montoImputado);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error procesando pago CC {Id} en Tango", pago.Id);
            return (false, 0m);
        }
    }

    // CONCEPTO en SBA04 es VARCHAR(40). Mantener formato corto y trazable por Id del pago en MySQL.
    private static string ConstruirConcepto(PagoCuentaCorrienteTangoDto pago)
    {
        var medio = string.IsNullOrWhiteSpace(pago.MedioPago) ? "MP" : pago.MedioPago;
        var concepto = $"{medio} cc #{pago.Id}";
        return concepto.Length > 40 ? concepto[..40] : concepto;
    }

    private static string Truncar(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length > max ? s[..max] : s);

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private List<ComprobanteImputable> ParseComprobantes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<ComprobanteImputable>>(json, JsonOpts) ?? new();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON de comprobantes inválido");
            return new();
        }
    }

    private sealed class SaldoComprobanteRow
    {
        public decimal Saldo { get; set; }
        public int? IdGva12 { get; set; }
    }
}
