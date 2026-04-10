using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Models;
using SyncService.Services;

namespace SyncService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly TangoInscripcionService _tangoInscripcionService;

    public Worker(ILogger<Worker> logger, IConfiguration config, TangoInscripcionService tangoInscripcionService)
    {
        _logger = logger;
        _config = config;
        _tangoInscripcionService = tangoInscripcionService;
        _http = new HttpClient();
        _http.BaseAddress = new Uri(_config["ApiSettings:BaseUrl"]!);
        _http.DefaultRequestHeaders.Add("X-Sync-Key", _config["ApiSettings:SyncKey"]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = _config.GetValue<int>("SyncIntervalSeconds", 30);
        _logger.LogInformation("SyncService iniciado. Intervalo: {Interval}s, API: {Url}", interval, _config["ApiSettings:BaseUrl"]);

        // Full sync on first run
        //await FullSyncAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando SyncQueue");
            }

            try
            {
                await SyncInscripcionesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando inscripciones a Tango");
            }

            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }

    private async Task ProcessQueueAsync()
    {
        using var conn = new SqlConnection(_config["SqlServerConnection"]);

        var items = (await conn.QueryAsync<SyncQueueItem>(
            "SELECT TOP 100 Id, Tabla, Operacion, ClaveValor FROM SyncQueue WHERE Procesado = 0 ORDER BY Id")).ToList();

        if (items.Count == 0) return;

        _logger.LogInformation("Procesando {Count} items de SyncQueue", items.Count);

        foreach (var item in items)
        {
            try
            {
                if (item.Operacion == "DELETE")
                {
                    await DeleteAsync(item.Tabla, item.ClaveValor);
                }
                else
                {
                    await UpsertAsync(conn, item.Tabla, item.ClaveValor);
                }

                await conn.ExecuteAsync("UPDATE SyncQueue SET Procesado = 1 WHERE Id = @Id", new { item.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sync item {Id}: {Tabla}/{Op}/{Clave}", item.Id, item.Tabla, item.Operacion, item.ClaveValor);
            }
        }

        // Limpiar procesados de más de 7 días
        await conn.ExecuteAsync("DELETE FROM SyncQueue WHERE Procesado = 1 AND FechaCreacion < DATEADD(DAY, -7, GETDATE())");
    }

    private async Task UpsertAsync(SqlConnection conn, string tabla, string clave)
    {
        switch (tabla)
        {
            case "Clientes":
                var cliente = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT CUIT AS Cuit, RAZON_SOCI AS RazonSoci, DOMICILIO AS Domicilio, C_POSTAL AS CodPostal, COD_PROVIN AS CodProvin FROM GVA14 WHERE CUIT = @Clave",
                    new { Clave = clave });
                if (cliente != null)
                    await PostAsync("/api/sync/clientes", new { cuit = (string)cliente.Cuit?.ToString().Trim(), razonSoci = (string)cliente.RazonSoci?.ToString().Trim(), domicilio = (string?)cliente.Domicilio?.ToString().Trim(), codPostal = (string?)cliente.CodPostal?.ToString().Trim(), codProvin = (string?)cliente.CodProvin?.ToString().Trim() });
                break;

            case "Articulos":
                var articulo = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT COD_ARTICU AS CodArticu, DESCRIPCIO AS Descripcio FROM STA11 WHERE COD_ARTICU = @Clave",
                    new { Clave = clave });
                if (articulo != null)
                    await PostAsync("/api/sync/articulos", new { codArticu = (string)articulo.CodArticu?.ToString().Trim(), descripcio = (string)articulo.Descripcio?.ToString().Trim() });
                break;

            case "Provincias":
                var provincia = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT COD_PROVIN AS Codigo, NOMBRE_PRO AS Nombre FROM GVA18 WHERE COD_PROVIN = @Clave",
                    new { Clave = clave });
                if (provincia != null)
                    await PostAsync("/api/sync/provincias", new { codigo = (string)provincia.Codigo?.ToString().Trim(), nombre = (string)provincia.Nombre?.ToString().Trim() });
                break;

            case "CuentaCorriente":
                await SyncCuentaCorrienteAsync(conn, clave);
                break;
        }
    }

    private async Task DeleteAsync(string tabla, string clave)
    {
        var endpoint = tabla switch
        {
            "Clientes" => $"/api/sync/clientes/{Uri.EscapeDataString(clave)}",
            "Articulos" => $"/api/sync/articulos/{Uri.EscapeDataString(clave)}",
            "Provincias" => $"/api/sync/provincias/{Uri.EscapeDataString(clave)}",
            _ => throw new InvalidOperationException($"Tabla desconocida: {tabla}")
        };
        await _http.DeleteAsync(endpoint);
    }

    private async Task PostAsync(string url, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("POST {Url} returned {Status}", url, response.StatusCode);
    }

    private async Task SyncCuentaCorrienteAsync(SqlConnection conn, string claveValor)
    {
        // claveValor = "T_COMP|N_COMP"
        var parts = claveValor.Split('|');
        if (parts.Length != 2) return;
        var tComp = parts[0];
        var nComp = parts[1];

        // Buscar COD_CLIENT en GVA12
        var codClient = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT COD_CLIENT FROM GVA12 WHERE T_COMP = @TComp AND N_COMP = @NComp",
            new { TComp = tComp, NComp = nComp });
        if (string.IsNullOrEmpty(codClient)) return;

        // Buscar CUIT en GVA14
        var cuit = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT CUIT FROM GVA14 WHERE COD_CLIENT = @CodClient",
            new { CodClient = codClient });
        if (string.IsNullOrEmpty(cuit)) return;

        await SyncResumenClienteAsync(conn, codClient.Trim(), cuit.Trim());
    }

    private async Task SyncResumenClienteAsync(SqlConnection conn, string codClient, string cuit)
    {
        const string sql = @"
            SELECT GVA46.T_COMP AS TComp, GVA46.N_COMP AS NComp, GVA46.FECHA_VTO AS FechaVto,
                   GVA46.IMPORTE_VT + SUM(ISNULL(IMPORT_CAN,0) * CASE WHEN TIPO_COMP = 'D' THEN 1 ELSE -1 END) AS Saldo
            FROM GVA46
            INNER JOIN GVA53 ON GVA53.T_COMP = GVA46.T_COMP AND GVA53.N_COMP = GVA46.N_COMP
            INNER JOIN STA11 ON STA11.COD_ARTICU = GVA53.COD_ARTICU AND STA11.DESCRIPCIO LIKE '%CUOTA%'
            LEFT OUTER JOIN GVA12 ON GVA12.T_COMP = GVA46.T_COMP AND GVA12.N_COMP = GVA46.N_COMP
            LEFT OUTER JOIN GVA07 IMPU ON IMPU.T_COMP = GVA46.T_COMP AND IMPU.N_COMP = GVA46.N_COMP AND IMPU.FECHA_VTO = GVA46.FECHA_VTO
            LEFT OUTER JOIN GVA15 ON GVA15.IDENT_COMP = IMPU.T_COMP_CAN
            WHERE COD_CLIENT = @CodClient
            GROUP BY GVA46.T_COMP, GVA46.N_COMP, GVA46.FECHA_VTO, GVA46.IMPORTE_VT
            HAVING GVA46.IMPORTE_VT + (SUM(ISNULL(IMPORT_CAN,0) * CASE WHEN TIPO_COMP = 'D' THEN 1 ELSE -1 END)) > 0";

        var movimientos = (await conn.QueryAsync<dynamic>(sql, new { CodClient = codClient }))
            .Select(m => new
            {
                tComp = ((string)m.TComp).Trim(),
                nComp = ((string)m.NComp).Trim(),
                fechaVto = (DateTime)m.FechaVto,
                saldo = (decimal)m.Saldo
            }).ToArray();

        await PostAsync("/api/sync/cuenta-corriente", new { cuit, movimientos });
        _logger.LogInformation("CuentaCorriente sync: cuit={Cuit}, movimientos={Count}", cuit, movimientos.Length);
    }

    private async Task FullSyncAsync()
    {
        _logger.LogInformation("Ejecutando sincronizacion completa...");
        using var conn = new SqlConnection(_config["SqlServerConnection"]);

        try
        {
            // Provincias
            var provincias = await conn.QueryAsync<dynamic>(
                "SELECT COD_PROVIN AS Codigo, NOMBRE_PRO AS Nombre FROM GVA18");
            var provCount = 0;
            foreach (var p in provincias)
            {
                await PostAsync("/api/sync/provincias", new { codigo = (string)p.Codigo?.ToString().Trim(), nombre = (string)p.Nombre?.ToString().Trim() });
                provCount++;
            }
            _logger.LogInformation("Provincias sincronizadas: {Count}", provCount);

            // Clientes
            var clientes = await conn.QueryAsync<dynamic>(
                "SELECT CUIT AS Cuit, RAZON_SOCI AS RazonSoci, DOMICILIO AS Domicilio, C_POSTAL AS CodPostal, COD_PROVIN AS CodProvin FROM GVA14 WHERE CUIT IS NOT NULL AND CUIT != ''");
            var cliCount = 0;
            foreach (var c in clientes)
            {
                await PostAsync("/api/sync/clientes", new { cuit = (string)c.Cuit?.ToString().Trim(), razonSoci = (string)c.RazonSoci?.ToString().Trim(), domicilio = (string?)c.Domicilio?.ToString().Trim(), codPostal = (string?)c.CodPostal?.ToString().Trim(), codProvin = (string?)c.CodProvin?.ToString().Trim() });
                cliCount++;
            }
            _logger.LogInformation("Clientes sincronizados: {Count}", cliCount);

            // Articulos
            var articulos = await conn.QueryAsync<dynamic>(
                "SELECT COD_ARTICU AS CodArticu, DESCRIPCIO AS Descripcio FROM STA11 WHERE COD_ARTICU IS NOT NULL AND COD_ARTICU != ''");
            var artCount = 0;
            foreach (var a in articulos)
            {
                await PostAsync("/api/sync/articulos", new { codArticu = (string)a.CodArticu?.ToString().Trim(), descripcio = (string)a.Descripcio?.ToString().Trim() });
                artCount++;
            }
            _logger.LogInformation("Articulos sincronizados: {Count}", artCount);

            // Cuenta Corriente — por cada cliente con comprobantes
            var clientesCuenta = await conn.QueryAsync<dynamic>(
                "SELECT DISTINCT GVA12.COD_CLIENT, GVA14.CUIT FROM GVA12 INNER JOIN GVA14 ON GVA14.COD_CLIENT = GVA12.COD_CLIENT WHERE GVA14.CUIT IS NOT NULL AND GVA14.CUIT != '' AND SALDO_CC <> 0");
            var ccCount = 0;
            foreach (var cc in clientesCuenta)
            {
                try
                {
                    await SyncResumenClienteAsync(conn, ((string)cc.COD_CLIENT).Trim(), ((string)cc.CUIT).Trim());
                    ccCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error sync cuenta corriente cliente {Cuit}", (string)cc.CUIT);
                }
            }
            _logger.LogInformation("Cuentas corrientes sincronizadas: {Count}", ccCount);

            _logger.LogInformation("Sincronizacion completa finalizada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sincronizacion completa");
        }
    }

    private async Task SyncInscripcionesAsync()
    {
        // 1. Obtener inscripciones confirmadas pendientes de sync a Tango
        var response = await _http.GetAsync("/api/sync/inscripciones");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GET /api/sync/inscripciones returned {Status}", response.StatusCode);
            return;
        }

        var inscripciones = await response.Content.ReadFromJsonAsync<List<InscripcionTangoDto>>();
        if (inscripciones == null || inscripciones.Count == 0) return;

        _logger.LogInformation("Procesando {Count} inscripciones pendientes de sync a Tango", inscripciones.Count);

        foreach (var insc in inscripciones)
        {
            try
            {
                // 2. Procesar en Tango (cada inscripción abre su propia conexión + transacción)
                using var conn = new SqlConnection(_config["SqlServerConnection"]);
                var ok = await _tangoInscripcionService.ProcesarInscripcionAsync(conn, insc);

                if (ok)
                {
                    // 3. Marcar como sincronizada en el backend
                    var patchResponse = await _http.PatchAsync(
                        $"/api/sync/inscripciones/{insc.Id}/tango",
                        new StringContent("", Encoding.UTF8, "application/json"));

                    if (!patchResponse.IsSuccessStatusCode)
                        _logger.LogWarning("No se pudo marcar inscripción {Id} como sincronizada: {Status}", insc.Id, patchResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando inscripción {Id} ({Nombre} {Apellido})", insc.Id, insc.Nombre, insc.Apellido);
            }
        }
    }
}

internal class SyncQueueItem
{
    public int Id { get; set; }
    public string Tabla { get; set; } = string.Empty;
    public string Operacion { get; set; } = string.Empty;
    public string ClaveValor { get; set; } = string.Empty;
}
