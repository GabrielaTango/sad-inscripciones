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

    private int TalonarioPedido => _config.GetValue("InscripcionSync:TalonarioPedido", 5);
    private int TalonarioFactura => _config.GetValue("InscripcionSync:TalonarioFactura", 8);
    private string CodigoVendedor => _config["InscripcionSync:CodigoVendedor"] ?? "90";
    private decimal CuentaHaber => _config.GetValue("InscripcionSync:CuentaHaber", 92M);
    private int IdSBA02 => _config.GetValue("InscripcionSync:IdSBA02", 7);
    private string TipoAsientoModelo => _config["InscripcionSync:TipoAsientoModelo"] ?? "02";

    public TangoInscripcionService(ILogger<TangoInscripcionService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<bool> ProcesarInscripcionAsync(SqlConnection conn, InscripcionTangoDto insc)
    {
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            var inscId = insc.Id.ToString();
            var razonSoci = $"{insc.Apellido}, {insc.Nombre}";
            var now = DateTime.Now;

            // 1. Buscar o crear cliente
            var codClient = await ExisteClienteAsync(conn, tx, insc.Documento ?? "");
            if (string.IsNullOrEmpty(codClient))
            {
                codClient = await TraerProximoClienteAsync(conn, tx);

                var gva14 = new TangoGVA14();
                gva14.Set("FILLER", codClient);
                gva14.Set("COD_CLIENT", codClient);
                gva14.Set("RAZON_SOCI", razonSoci);
                gva14.Set("NOM_COM", razonSoci);
                gva14.Set("DOMICILIO", insc.Domicilio ?? "");
                gva14.Set("C_POSTAL", insc.CodigoPostal ?? "");
                gva14.Set("LOCALIDAD", insc.Localidad ?? "");
                gva14.Set("COD_PROVIN", insc.Provincia ?? "");
                gva14.Set("E_MAIL", insc.Email);
                gva14.Set("TELEFONO_1", insc.Telefono ?? "");
                gva14.Set("TELEFONO_2", insc.Celular ?? "");
                gva14.Set("CUIT", insc.Documento ?? "");
                gva14.Set("COD_GVA14", codClient);
                gva14.SetDate("FECHA_ALTA", now);
                await conn.ExecuteAsync(gva14.Insert(), transaction: tx);

                _logger.LogInformation("Cliente creado: {CodClient} - {Razon}", codClient, razonSoci);
            }

            // 2. Crear pedido (GVA21)
            var nroPedido = await TraerProximoNCompAsync(conn, tx, TalonarioPedido);
            var idAsientoModelo = await TraerIdAsientoModeloAsync(conn, tx, TipoAsientoModelo);
            var codArticu = insc.CodArticu ?? "";

            var gva21 = new TangoGVA21();
            gva21.Set("FILLER", codClient);
            gva21.Set("COD_CLIENT", codClient);
            gva21.Set("COD_VENDED", CodigoVendedor);
            gva21.Set("NRO_PEDIDO", nroPedido);
            gva21.SetInt("TALON_PED", TalonarioPedido);
            gva21.SetInt("TALONARIO", TalonarioFactura);
            gva21.SetDate("FECHA_PEDI", now);
            gva21.SetDate("FECHA_INGRESO", now);
            gva21.SetDate("FECHA_ENTR", now);
            gva21.Set("LEYENDA_1", "FACINSCRIP");
            gva21.Set("LEYENDA_2", inscId);
            gva21.SetDecimal("TOTAL_PEDI", insc.PrecioFinal);
            gva21.SetDecimal("TOTAL_PEDI_CON_IMPUESTOS", insc.PrecioFinal);
            gva21.SetInt("ID_ASIENTO_MODELO_GV", idAsientoModelo);
            gva21.SetDate("FECHA_ULTIMA_MODIFICACION", now);
            gva21.SetInt("ESTADO", 1);
            await conn.ExecuteAsync(gva21.Insert(), transaction: tx);

            // 3. Crear renglón (GVA03) con cálculo de precios
            var impuesto = await TraerImpuestosAsync(conn, tx, codArticu);

            var gva03 = new TangoGVA03();
            gva03.Set("FILLER", codArticu);
            gva03.Set("COD_ARTICU", codArticu);
            gva03.Set("NRO_PEDIDO", nroPedido);
            gva03.SetInt("TALON_PED", TalonarioPedido);
            gva03.CalcularPrecio(insc.PrecioFinal, impuesto);
            await conn.ExecuteAsync(gva03.Insert(), transaction: tx);

            // Actualizar próximo nro pedido
            await conn.ExecuteAsync(BuildUpdateProximo(nroPedido, TalonarioPedido), transaction: tx);

            // 4. Crear factura (GVA12)
            var nCompFactura = await TraerProximoNCompAsync(conn, tx, TalonarioFactura);

            var gva12 = new TangoGVA12();
            gva12.Set("FILLER", inscId);
            gva12.Set("COD_CLIENT", codClient);
            gva12.Set("COD_VENDED", CodigoVendedor);
            gva12.Set("N_COMP", nCompFactura);
            gva12.SetInt("TALONARIO", TalonarioFactura);
            gva12.SetDate("FECHA_EMIS", now);
            gva12.SetDate("FECHA_INGRESO", now);
            gva12.SetDecimal("IMPORTE", insc.PrecioFinal);
            gva12.SetDecimal("UNIDADES", insc.PrecioFinal);
            gva12.Set("ESTADO", "CTA");
            gva12.Set("ESTADO_UNI", "CTA");
            gva12.Set("LEYENDA_1", "FACINSCRIP");
            gva12.Set("LEYENDA_2", inscId);
            gva12.SetInt("ID_ASIENTO_MODELO_GV", idAsientoModelo);
            await conn.ExecuteAsync(gva12.Insert(), transaction: tx);

            // 5. Actualizar saldo del cliente
            await conn.ExecuteAsync(gva12.UpdateSaldoCliente(), transaction: tx);

            // 6. Crear comprobante de tesorería (SBA04)
            var nInterno = await TraerProximoNInternoAsync(conn, tx);

            var sba04 = new TangoSBA04();
            sba04.Set("FILLER", inscId);
            sba04.Set("N_COMP", nCompFactura);
            sba04.Set("COD_COMP", "REC");
            sba04.SetInt("N_INTERNO", nInterno);
            sba04.SetDate("FECHA", now);
            sba04.SetDate("FECHA_EMIS", now);
            sba04.SetDate("FECHA_ING", now);
            sba04.Set("COD_GVA14", codClient);
            sba04.SetDecimal("TOTAL_IMPORTE_CTE", insc.PrecioFinal);
            sba04.SetDecimal("TOTAL_IMPORTE_EXT", insc.PrecioFinal);
            sba04.Set("CONCEPTO", "Factura Inscripcion");
            sba04.SetInt("ID_SBA02", IdSBA02);
            sba04.SetDate("FECHA_ULTIMA_MODIFICACION", new DateTime(1, 1, 1));
            await conn.ExecuteAsync(sba04.Insert(), transaction: tx);

            // 7. Crear registro de asiento contable
            await conn.ExecuteAsync(sba04.InsertAsientoComprobante(), transaction: tx);

            // 8. Crear SBA05 Haber
            var sba05h = new TangoSBA05();
            sba05h.ConfigurarDH("H");
            sba05h.Set("FILLER", inscId);
            sba05h.Set("N_COMP", nCompFactura);
            sba05h.Set("COD_COMP", "REC");
            sba05h.SetDecimal("COD_CTA", CuentaHaber);
            sba05h.SetDecimal("MONTO", insc.PrecioFinal);
            sba05h.SetDecimal("CANT_MONE", insc.PrecioFinal);
            sba05h.SetDecimal("UNIDADES", insc.PrecioFinal);
            sba05h.SetDate("FECHA", now);
            sba05h.Set("COD_GVA14", codClient);
            sba05h.SetInt("ID_SBA02", IdSBA02);
            sba05h.SetDate("F_CONC_EFT", new DateTime(1, 1, 1));

            await conn.ExecuteAsync(sba05h.Insert(), transaction: tx);
            await conn.ExecuteAsync(sba05h.UpdateSBA01(), transaction: tx);

            // 9. Crear SBA05 Debe
            var cuentaDebe = await TraerCuentaDebeAsync(conn, tx, codClient);

            var sba05d = new TangoSBA05();
            sba05d.ConfigurarDH("D");
            sba05d.Set("FILLER", inscId);
            sba05d.Set("N_COMP", nCompFactura);
            sba05d.Set("COD_COMP", "REC");
            sba05d.SetDecimal("COD_CTA", cuentaDebe);
            sba05d.SetDecimal("MONTO", insc.PrecioFinal);
            sba05d.SetDecimal("CANT_MONE", insc.PrecioFinal);
            sba05d.SetDecimal("UNIDADES", insc.PrecioFinal);
            sba05d.SetDate("FECHA", now);
            sba05d.Set("COD_GVA14", codClient);
            sba05d.SetInt("ID_SBA02", IdSBA02);
            sba05d.SetDate("F_CONC_EFT", new DateTime(1, 1, 1));

            await conn.ExecuteAsync(sba05d.Insert(), transaction: tx);
            await conn.ExecuteAsync(sba05d.UpdateSBA01(), transaction: tx);

            // 10. Actualizar próximo nro factura
            await conn.ExecuteAsync(BuildUpdateProximo(nCompFactura, TalonarioFactura), transaction: tx);

            // 11. Actualizar INCREMENTAL_VALUE
            await conn.ExecuteAsync(
                "UPDATE INCREMENTAL_VALUE SET ULTIMOVALOR = (SELECT MAX(N_INTERNO) FROM SBA04) WHERE TABLA = 'SBA04' AND CAMPO = 'N_INTERNO'",
                transaction: tx);

            tx.Commit();
            _logger.LogInformation("Inscripción {Id} procesada OK: cliente={CodClient}, factura={NComp}", insc.Id, codClient, nCompFactura);
            return true;
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error procesando inscripción {Id} en Tango", insc.Id);
            return false;
        }
    }

    // ===== Helpers de consulta =====

    private async Task<string?> ExisteClienteAsync(SqlConnection conn, SqlTransaction tx, string cuit)
    {
        if (string.IsNullOrWhiteSpace(cuit)) return null;
        return await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT TOP 1 COD_CLIENT FROM GVA14 WHERE CUIT = @Cuit", new { Cuit = cuit }, tx);
    }

    private async Task<string> TraerProximoClienteAsync(SqlConnection conn, SqlTransaction tx)
    {
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT SUBSTRING(MAX(COD_CLIENT),1,1) AS LETRA, SUBSTRING(MAX(COD_CLIENT),2,5) + 1 AS PROXIMO
              FROM GVA14 WHERE COD_CLIENT > 'P00000' AND LEN(COD_CLIENT) = 6", transaction: tx);

        if (result == null) return "P00001";
        string letras = "PQRSTUVW";
        string letra = ((string)result.LETRA).Trim();
        int proximo = (int)result.PROXIMO;
        if (proximo > 99999)
        {
            proximo = 1;
            letra = letras[letras.IndexOf(letra[0]) + 1].ToString();
        }
        return letra + proximo.ToString().PadLeft(5, '0');
    }

    private async Task<string> TraerProximoNCompAsync(SqlConnection conn, SqlTransaction tx, int talonario)
    {
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT TIPO, SUCURSAL, PROXIMO FROM GVA43 WHERE TALONARIO = @Tal", new { Tal = talonario }, tx);
        if (row == null) throw new InvalidOperationException($"Talonario {talonario} no encontrado en GVA43");

        string tipo = ((string)row.TIPO).PadLeft(1, ' ');
        string sucursal = (string)row.SUCURSAL;
        string proximoDecrypted = SqlH.DocNumberDecrypt((string)row.PROXIMO).PadLeft(8, '0');
        return tipo + sucursal + proximoDecrypted;
    }

    private async Task<int> TraerProximoNInternoAsync(SqlConnection conn, SqlTransaction tx)
    {
        var max = await conn.QueryFirstOrDefaultAsync<decimal?>("SELECT CAST(MAX(N_INTERNO) AS DECIMAL) FROM SBA04", transaction: tx);
        return (int)(max ?? 0) + 1;
    }

    private async Task<int> TraerIdAsientoModeloAsync(SqlConnection conn, SqlTransaction tx, string tipo)
    {
        return await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT ID_ASIENTO_MODELO_GV FROM ASIENTO_MODELO_GV WHERE COD_ASIENTO_MODELO_GV = @Tipo",
            new { Tipo = tipo }, tx) ?? -1;
    }

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

    private async Task<decimal> TraerCuentaDebeAsync(SqlConnection conn, SqlTransaction tx, string codClient)
    {
        var cta = await conn.QueryFirstOrDefaultAsync<decimal?>(
            @"SELECT CAST(t2.CAMPOS_ADICIONALES.value('(/CAMPOS_ADICIONALES/CA_1118_CTA_CUOTAS)[1]', 'varchar(10)') AS DECIMAL)
              FROM GVA14 t1 INNER JOIN GVA23 t2 ON t1.COD_VENDED = t2.COD_VENDED
              WHERE t1.COD_CLIENT = @CodClient AND t1.COD_VENDED <> '90'",
            new { CodClient = codClient }, tx);
        return cta ?? CuentaHaber;
    }

    private string BuildUpdateProximo(string nComp, int talonario)
    {
        long prox = long.Parse(nComp.Substring(6, 8));
        string encrypted = SqlH.DocNumberEncrypt((prox + 1).ToString().PadLeft(8, '0'));
        return $"UPDATE GVA43 SET PROXIMO = '{encrypted}' WHERE TALONARIO = {talonario}";
    }
}
