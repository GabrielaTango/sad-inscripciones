using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Models;

namespace SyncService.Services;

/// <summary>
/// Gestiona el alta/lookup del cliente en Tango (GVA14).
/// Usado tanto por TangoInscripcionService (al crear pedido/factura) como
/// por TangoPagoService (al crear recibo sin inscripción confirmada).
/// </summary>
public class TangoClienteService
{
    private readonly ILogger<TangoClienteService> _logger;

    public TangoClienteService(ILogger<TangoClienteService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Devuelve el COD_CLIENT para el CUIT/documento dado.
    /// Si no existe, lo crea en GVA14 con los datos provistos.
    /// Usa UPDLOCK + HOLDLOCK para serializar creaciones concurrentes del mismo CUIT.
    /// </summary>
    public async Task<string> EnsureClienteAsync(SqlConnection conn, SqlTransaction tx, DatosClienteTango datos)
    {
        var cuit = (datos.Documento ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cuit))
            throw new InvalidOperationException("Documento (CUIT) requerido para crear/obtener cliente en Tango");

        var existente = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT TOP 1 COD_CLIENT FROM GVA14 WITH (UPDLOCK, HOLDLOCK) WHERE CUIT = @Cuit AND FECHA_INHA = '1800-01-01'",
            new { Cuit = cuit }, tx);

        if (!string.IsNullOrWhiteSpace(existente))
            return existente.Trim();

        var codClient = await TraerProximoCodClientAsync(conn, tx);
        var razonSoci = $"{datos.Apellido}, {datos.Nombre}";
        var now = DateTime.Now;

        var gva14 = new TangoGVA14();
        gva14.Set("FILLER", codClient);
        gva14.Set("COD_CLIENT", codClient);
        gva14.Set("RAZON_SOCI", razonSoci);
        gva14.Set("NOM_COM", razonSoci);
        gva14.Set("DOMICILIO", datos.Domicilio ?? "");
        gva14.Set("C_POSTAL", datos.CodigoPostal ?? "");
        gva14.Set("LOCALIDAD", datos.Localidad ?? "");
        gva14.Set("COD_PROVIN", datos.Provincia ?? "");
        gva14.Set("E_MAIL", datos.Email);
        gva14.Set("TELEFONO_1", datos.Telefono ?? "");
        gva14.Set("TELEFONO_2", datos.Celular ?? "");
        gva14.Set("CUIT", cuit);
        gva14.Set("COD_GVA14", codClient);
        gva14.SetDate("FECHA_ALTA", now);
        await conn.ExecuteAsync(gva14.Insert(), transaction: tx);

        // Dirección de entrega habitual ("PRINCIPAL") con los mismos datos.
        var direccion = new TangoDIRECCION_ENTREGA();
        direccion.Set("COD_CLIENTE", codClient);
        direccion.Set("DIRECCION", string.IsNullOrWhiteSpace(datos.Domicilio) ? "INSCRIPCION" : datos.Domicilio);
        direccion.Set("COD_PROVINCIA", datos.Provincia ?? "00");
        direccion.Set("LOCALIDAD", datos.Localidad ?? "");
        direccion.Set("CODIGO_POSTAL", datos.CodigoPostal ?? "");
        direccion.Set("TELEFONO1", datos.Telefono ?? "");
        direccion.Set("TELEFONO2", datos.Celular ?? "");
        await conn.ExecuteAsync(direccion.Insert(), transaction: tx);

        _logger.LogInformation("Cliente creado en Tango: {CodClient} - {Razon} (CUIT {Cuit})", codClient, razonSoci, cuit);
        return codClient;
    }

    private static async Task<string> TraerProximoCodClientAsync(SqlConnection conn, SqlTransaction tx)
    {
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT SUBSTRING(MAX(COD_CLIENT),1,1) AS LETRA, SUBSTRING(MAX(COD_CLIENT),2,5) + 1 AS PROXIMO
              FROM GVA14 WHERE COD_CLIENT > 'P00000' AND LEN(COD_CLIENT) = 6", transaction: tx);

        if (result == null) return "P00001";
        const string letras = "PQRSTUVW";
        var letra = ((string)result.LETRA).Trim();
        var proximo = (int)result.PROXIMO;
        if (proximo > 99999)
        {
            proximo = 1;
            letra = letras[letras.IndexOf(letra[0]) + 1].ToString();
        }
        return letra + proximo.ToString().PadLeft(5, '0');
    }
}
