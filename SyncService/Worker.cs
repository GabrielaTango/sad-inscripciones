using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Helpers;
using SyncService.Models;
using SyncService.Services;

namespace SyncService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly TangoInscripcionService _tangoInscripcionService;
    private readonly TangoPagoService _tangoPagoService;
    private readonly TangoImputacionService _tangoImputacionService;
    private readonly TangoResumenCuentaService _tangoResumenCuentaService;
    private readonly ChangeTrackingService _ctService;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration config,
        TangoInscripcionService tangoInscripcionService,
        TangoPagoService tangoPagoService,
        TangoImputacionService tangoImputacionService,
        TangoResumenCuentaService tangoResumenCuentaService,
        ChangeTrackingService ctService)
    {
        _logger = logger;
        _config = config;
        _tangoInscripcionService = tangoInscripcionService;
        _tangoPagoService = tangoPagoService;
        _tangoImputacionService = tangoImputacionService;
        _tangoResumenCuentaService = tangoResumenCuentaService;
        _ctService = ctService;
        _http = new HttpClient();
        _http.BaseAddress = new Uri(_config["ApiSettings:BaseUrl"]!);
        _http.DefaultRequestHeaders.Add("X-Sync-Key", _config["ApiSettings:SyncKey"]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = _config.GetValue<int>("SyncIntervalMinutes", 60) * 60; //Convert to Seconds
        var triggerPollSeconds = _config.GetValue<int>("TriggerPollSeconds", 30);
        _logger.LogInformation(
            "SyncService iniciado. Intervalo ciclo: {Interval}s, poll trigger: {Poll}s, API: {Url}",
            interval, triggerPollSeconds, _config["ApiSettings:BaseUrl"]);

        await EnsureChangeTrackingAsync(stoppingToken);
        await EnsureTangoSchemaCacheAsync(stoppingToken);

        var nextCycleAt = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await IsFullSyncTriggeredAsync(stoppingToken))
                {
                    _logger.LogInformation("Full sync solicitado por trigger manual.");
                    await FullSyncAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error chequeando/ejecutando trigger de full sync");
            }

            if (DateTime.UtcNow >= nextCycleAt)
            {
                await RunIncrementalCycleAsync(stoppingToken);
                nextCycleAt = DateTime.UtcNow.AddSeconds(interval);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(triggerPollSeconds), stoppingToken);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunIncrementalCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ProcessChangeTrackingAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando Change Tracking");
        }

        try
        {
            await SyncInscripcionesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando inscripciones a Tango");
        }

        try
        {
            await SyncPagosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando pagos a Tango");
        }

        try
        {
            await _tangoImputacionService.EjecutarPendientesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando imputaciones pendientes en Tango");
        }

        try
        {
            await SyncPagosCuentaCorrienteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando pagos de cuenta corriente a Tango");
        }
    }

    private async Task<bool> IsFullSyncTriggeredAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/api/sync/trigger", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GET /api/sync/trigger returned {Status}", response.StatusCode);
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<TriggerResponse>(cancellationToken: ct);
        return payload?.Pending == true;
    }

    private record TriggerResponse(bool Pending, DateTime? RequestedAt);

    private async Task SyncPagosAsync()
    {
        var response = await _http.GetAsync("/api/sync/pagos");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GET /api/sync/pagos returned {Status}", response.StatusCode);
            return;
        }

        var pagos = await response.Content.ReadFromJsonAsync<List<PagoTangoDto>>();
        if (pagos == null || pagos.Count == 0) return;

        _logger.LogInformation("Procesando {Count} pagos pendientes de sync a Tango", pagos.Count);

        foreach (var pago in pagos)
        {
            try
            {
                using var conn = new SqlConnection(_config["SqlServerConnection"]);
                var ok = await _tangoPagoService.ProcesarPagoAsync(conn, pago);

                if (ok)
                {
                    var patch = await _http.PatchAsync(
                        $"/api/sync/pagos/{pago.Id}/tango",
                        new StringContent("", Encoding.UTF8, "application/json"));

                    if (!patch.IsSuccessStatusCode)
                        _logger.LogWarning("No se pudo marcar pago {Id} como sincronizado: {Status}", pago.Id, patch.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando pago {Id} (insc {InscId})", pago.Id, pago.InscripcionId);
            }
        }
    }

    private async Task SyncPagosCuentaCorrienteAsync()
    {
        var response = await _http.GetAsync("/api/sync/pagos-cuenta-corriente");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GET /api/sync/pagos-cuenta-corriente returned {Status}", response.StatusCode);
            return;
        }

        var pagos = await response.Content.ReadFromJsonAsync<List<PagoCuentaCorrienteTangoDto>>();
        if (pagos == null || pagos.Count == 0) return;

        _logger.LogInformation("Procesando {Count} pagos CC pendientes de sync a Tango", pagos.Count);

        foreach (var pago in pagos)
        {
            try
            {
                using var conn = new SqlConnection(_config["SqlServerConnection"]);
                var (ok, montoImputado) = await _tangoResumenCuentaService.ProcesarPagoAsync(conn, pago);

                if (ok)
                {
                    var patch = await _http.PatchAsync(
                        $"/api/sync/pagos-cuenta-corriente/{pago.Id}/tango",
                        new StringContent(
                            JsonSerializer.Serialize(new { montoImputado }),
                            Encoding.UTF8, "application/json"));

                    if (!patch.IsSuccessStatusCode)
                        _logger.LogWarning("No se pudo marcar pago CC {Id} como sincronizado: {Status}", pago.Id, patch.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando pago CC {Id} (cuit {Cuit})", pago.Id, pago.Cuit);
            }
        }
    }

    private async Task EnsureTangoSchemaCacheAsync(CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_config["SqlServerConnection"]);
            await conn.OpenAsync(ct);
            await TangoSchemaCache.LoadAllAsync(conn, ct);
            _logger.LogInformation("TangoSchemaCache cargado para {Count} tablas", TangoSchemaCache.TablasInsertables.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error precargando TangoSchemaCache. Los INSERTs de Tango van a fallar hasta resolver esto.");
        }
    }

    private async Task EnsureChangeTrackingAsync(CancellationToken ct)
    {
        try
        {
            await _ctService.EnsureSyncStateTableAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo crear/validar la tabla SyncState");
        }

        foreach (var logical in ChangeTrackingService.Tables.Keys)
        {
            try
            {
                await _ctService.EnsureChangeTrackingEnabledAsync(logical, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo habilitar Change Tracking para {Table}", logical);
            }
        }
    }

    private async Task ProcessChangeTrackingAsync(CancellationToken ct)
    {
        using var conn = new SqlConnection(_config["SqlServerConnection"]);
        await conn.OpenAsync(ct);

        foreach (var logical in ChangeTrackingService.Tables.Keys)
        {
            try
            {
                await ProcessTableAsync(conn, logical, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando CT para {Table}", logical);
            }
        }
    }

    private async Task ProcessTableAsync(SqlConnection conn, string logical, CancellationToken ct)
    {
        var lastVersion = await conn.ExecuteScalarAsync<long?>(new CommandDefinition(
            "SELECT LastVersion FROM SyncState WHERE Tabla = @Tabla",
            new { Tabla = logical },
            cancellationToken: ct));

        if (lastVersion is null)
        {
            var current = await _ctService.GetCurrentVersionAsync(ct) ?? 0L;
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO SyncState (Tabla, LastVersion) VALUES (@Tabla, @Version)",
                new { Tabla = logical, Version = current },
                cancellationToken: ct));
            _logger.LogInformation("SyncState inicializado para {Table} en v{Version}", logical, current);
            return;
        }

        if (!await _ctService.IsVersionValidAsync(logical, lastVersion.Value, ct))
        {
            var current = await _ctService.GetCurrentVersionAsync(ct) ?? 0L;
            _logger.LogWarning(
                "LastVersion {Old} de {Table} es inválida (retención CT expirada). Reseteando a v{New}; se pierde el historial intermedio.",
                lastVersion, logical, current);
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE SyncState SET LastVersion = @Version, UpdatedAt = SYSUTCDATETIME() WHERE Tabla = @Tabla",
                new { Tabla = logical, Version = current },
                cancellationToken: ct));
            return;
        }

        var changes = await _ctService.GetChangesAsync(logical, lastVersion.Value, ct);
        if (changes.Count == 0) return;

        var dispatchTable = logical switch
        {
            "CuentaCorrienteComprobantes" or "CuentaCorrienteImputaciones" => "CuentaCorriente",
            _ => logical
        };

        var deduped = changes
            .GroupBy(c => c.ClaveValor)
            .Select(g => g.OrderByDescending(c => c.Version).First())
            .ToList();

        _logger.LogInformation("CT {Table}: {Unique} cambios únicos ({Total} raw) desde v{Version}",
            logical, deduped.Count, changes.Count, lastVersion);

        foreach (var change in deduped)
        {
            try
            {
                await UpsertAsync(conn, dispatchTable, change.ClaveValor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sync CT {Table}/{Clave}", logical, change.ClaveValor);
            }
        }

        var maxVersion = changes.Max(c => c.Version);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE SyncState SET LastVersion = @Version, UpdatedAt = SYSUTCDATETIME() WHERE Tabla = @Tabla",
            new { Tabla = logical, Version = maxVersion },
            cancellationToken: ct));
    }

    private async Task UpsertAsync(SqlConnection conn, string tabla, string clave)
    {
        switch (tabla)
        {
            case "Clientes":
                var cliente = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT CUIT AS Cuit, RAZON_SOCI AS RazonSoci, DOMICILIO AS Domicilio, C_POSTAL AS CodPostal, COD_PROVIN AS CodProvin, COD_VENDED AS CodVended, COD_CLIENT AS CodClient FROM GVA14 WHERE CUIT = @Clave",
                    new { Clave = clave });
                if (cliente != null)
                    await PostAsync("/api/sync/clientes", new { cuit = (string)cliente.Cuit?.ToString().Trim(), razonSoci = (string)cliente.RazonSoci?.ToString().Trim(), domicilio = (string?)cliente.Domicilio?.ToString().Trim(), codPostal = (string?)cliente.CodPostal?.ToString().Trim(), codProvin = (string?)cliente.CodProvin?.ToString().Trim(), codVended = (string?)cliente.CodVended?.ToString().Trim(), codClient = (string?)cliente.CodClient?.ToString().Trim() });
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

            case "Vendedores":
                var vendedor = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT
                        COD_VENDED AS CodVended,
                        ISNULL(CAMPOS_ADICIONALES.value('(CAMPOS_ADICIONALES/CA_1118_CTA_CAJA)[1]', 'INT'), 0) AS CtaCaja,
                        ISNULL(CAMPOS_ADICIONALES.value('(CAMPOS_ADICIONALES/CA_1118_CTA_TRANSFERENCIA)[1]', 'INT'), 0) AS CtaTransferencia,
                        ISNULL(CAMPOS_ADICIONALES.value('(CAMPOS_ADICIONALES/CA_1118_CTA_CUOTAS)[1]', 'INT'), 0) AS CtaCuotas,
                        ISNULL(CAMPOS_ADICIONALES.value('(CAMPOS_ADICIONALES/CA_1118_CTA_OTRA)[1]', 'INT'), 0) AS CtaOtra
                    FROM GVA23
                    WHERE COD_VENDED = @Clave",
                    new { Clave = clave });
                if (vendedor != null)
                    await PostAsync("/api/sync/vendedores", new
                    {
                        codVended = ((string)vendedor.CodVended).Trim(),
                        ctaCaja = (int)vendedor.CtaCaja,
                        ctaTransferencia = (int)vendedor.CtaTransferencia,
                        ctaCuotas = (int)vendedor.CtaCuotas,
                        ctaOtra = (int)vendedor.CtaOtra,
                    });
                break;
        }
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

    private const int FullSyncBatchSize = 500;
    private const int FullSyncCcParallelism = 8;

    private async Task FullSyncAsync()
    {
        _logger.LogInformation("Ejecutando sincronizacion completa...");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var conn = new SqlConnection(_config["SqlServerConnection"]);

        try
        {
            // Provincias (batch)
            var provincias = (await conn.QueryAsync<dynamic>(
                "SELECT COD_PROVIN AS Codigo, NOMBRE_PRO AS Nombre FROM GVA18"))
                .Select(p => new
                {
                    codigo = ((string?)p.Codigo?.ToString())?.Trim(),
                    nombre = ((string?)p.Nombre?.ToString())?.Trim(),
                })
                .Where(p => !string.IsNullOrEmpty(p.codigo))
                .ToList();
            await PostBatchedAsync("/api/sync/provincias-batch", provincias, "Provincias");

            // Clientes (batch)
            var clientes = (await conn.QueryAsync<dynamic>(
                "SELECT CUIT AS Cuit, RAZON_SOCI AS RazonSoci, DOMICILIO AS Domicilio, C_POSTAL AS CodPostal, COD_PROVIN AS CodProvin, COD_VENDED AS CodVended, COD_CLIENT AS CodClient FROM GVA14 WHERE CUIT IS NOT NULL AND CUIT != ''"))
                .Select(c => new
                {
                    cuit = ((string?)c.Cuit?.ToString())?.Trim(),
                    razonSoci = ((string?)c.RazonSoci?.ToString())?.Trim(),
                    domicilio = ((string?)c.Domicilio?.ToString())?.Trim(),
                    codPostal = ((string?)c.CodPostal?.ToString())?.Trim(),
                    codProvin = ((string?)c.CodProvin?.ToString())?.Trim(),
                    codVended = ((string?)c.CodVended?.ToString())?.Trim(),
                    codClient = ((string?)c.CodClient?.ToString())?.Trim(),
                })
                .Where(c => !string.IsNullOrEmpty(c.cuit))
                .ToList();
            await PostBatchedAsync("/api/sync/clientes-batch", clientes, "Clientes");

            // Articulos (batch)
            var articulos = (await conn.QueryAsync<dynamic>(
                "SELECT COD_ARTICU AS CodArticu, DESCRIPCIO AS Descripcio FROM STA11 WHERE COD_ARTICU IS NOT NULL AND COD_ARTICU != ''"))
                .Select(a => new
                {
                    codArticu = ((string?)a.CodArticu?.ToString())?.Trim(),
                    descripcio = ((string?)a.Descripcio?.ToString())?.Trim(),
                })
                .Where(a => !string.IsNullOrEmpty(a.codArticu))
                .ToList();
            await PostBatchedAsync("/api/sync/articulos-batch", articulos, "Articulos");

            // Vendedores (batch)
            var vendedores = (await conn.QueryAsync<dynamic>(@"
                SELECT
                    COD_VENDED AS CodVended,
                    ISNULL(CAMPOS_ADICIONALES.value('(CAMPOS_ADICIONALES/CA_1118_CTA_CAJA)[1]', 'INT'), 0) AS CtaCaja,
                    ISNULL(CAMPOS_ADICIONALES.value('(CAMPOS_ADICIONALES/CA_1118_CTA_TRANSFERENCIA)[1]', 'INT'), 0) AS CtaTransferencia,
                    ISNULL(CAMPOS_ADICIONALES.value('(CAMPOS_ADICIONALES/CA_1118_CTA_CUOTAS)[1]', 'INT'), 0) AS CtaCuotas,
                    ISNULL(CAMPOS_ADICIONALES.value('(CAMPOS_ADICIONALES/CA_1118_CTA_OTRA)[1]', 'INT'), 0) AS CtaOtra
                FROM GVA23
                WHERE COD_VENDED IS NOT NULL AND COD_VENDED <> ''"))
                .Select(v => new
                {
                    codVended = ((string)v.CodVended).Trim(),
                    ctaCaja = (int)v.CtaCaja,
                    ctaTransferencia = (int)v.CtaTransferencia,
                    ctaCuotas = (int)v.CtaCuotas,
                    ctaOtra = (int)v.CtaOtra,
                })
                .ToList();
            await PostBatchedAsync("/api/sync/vendedores-batch", vendedores, "Vendedores");

            // Cuenta Corriente — query pesada por cliente, paralelizamos
            var clientesCuenta = (await conn.QueryAsync<dynamic>(
                "SELECT DISTINCT GVA12.COD_CLIENT, GVA14.CUIT FROM GVA12 INNER JOIN GVA14 ON GVA14.COD_CLIENT = GVA12.COD_CLIENT WHERE GVA14.CUIT IS NOT NULL AND GVA14.CUIT != '' AND SALDO_CC <> 0"))
                .Select(cc => new
                {
                    CodClient = ((string)cc.COD_CLIENT).Trim(),
                    Cuit = ((string)cc.CUIT).Trim(),
                })
                .ToList();

            var connStr = _config["SqlServerConnection"]!;
            var ccCount = 0;
            await Parallel.ForEachAsync(
                clientesCuenta,
                new ParallelOptions { MaxDegreeOfParallelism = FullSyncCcParallelism },
                async (cc, ct) =>
                {
                    try
                    {
                        await using var localConn = new SqlConnection(connStr);
                        await localConn.OpenAsync(ct);
                        await SyncResumenClienteAsync(localConn, cc.CodClient, cc.Cuit);
                        Interlocked.Increment(ref ccCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error sync cuenta corriente cliente {Cuit}", cc.Cuit);
                    }
                });
            _logger.LogInformation("Cuentas corrientes sincronizadas: {Count} de {Total}", ccCount, clientesCuenta.Count);

            sw.Stop();
            _logger.LogInformation("Sincronizacion completa finalizada en {Elapsed}.", sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sincronizacion completa");
        }
    }

    private async Task PostBatchedAsync<T>(string url, IReadOnlyList<T> items, string label)
    {
        if (items.Count == 0)
        {
            _logger.LogInformation("{Label} sincronizados: 0", label);
            return;
        }

        var total = 0;
        for (var i = 0; i < items.Count; i += FullSyncBatchSize)
        {
            var chunk = items.Skip(i).Take(FullSyncBatchSize).ToArray();
            await PostAsync(url, chunk);
            total += chunk.Length;
            _logger.LogDebug("{Label} batch enviado: {Sent}/{Total}", label, total, items.Count);
        }
        _logger.LogInformation("{Label} sincronizados: {Count}", label, total);
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
