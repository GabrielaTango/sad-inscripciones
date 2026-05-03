using Dapper;
using Microsoft.Data.SqlClient;

namespace SyncService.Services;

public record ChangeRecord(long Version, char Operation, string ClaveValor);

public record TableSyncConfig(
    string LogicalName,
    string SourceTable,
    string KeyColumn,
    string BusinessKeySelect,
    string? ExtraFilter = null);

public class ChangeTrackingService
{
    public static readonly IReadOnlyDictionary<string, TableSyncConfig> Tables =
        new Dictionary<string, TableSyncConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["Clientes"] = new(
                LogicalName: "Clientes",
                SourceTable: "GVA14",
                KeyColumn: "ID_GVA14",
                BusinessKeySelect: "RTRIM(T.CUIT)",
                ExtraFilter: "T.CUIT IS NOT NULL AND T.CUIT <> ''"),

            ["Articulos"] = new(
                LogicalName: "Articulos",
                SourceTable: "STA11",
                KeyColumn: "ID_STA11",
                BusinessKeySelect: "RTRIM(T.COD_ARTICU)",
                ExtraFilter: "T.COD_ARTICU IS NOT NULL AND T.COD_ARTICU <> ''"),

            ["Provincias"] = new(
                LogicalName: "Provincias",
                SourceTable: "GVA18",
                KeyColumn: "ID_GVA18",
                BusinessKeySelect: "RTRIM(T.COD_PROVIN)",
                ExtraFilter: "T.COD_PROVIN IS NOT NULL AND T.COD_PROVIN <> ''"),

            ["CuentaCorrienteComprobantes"] = new(
                LogicalName: "CuentaCorrienteComprobantes",
                SourceTable: "GVA12",
                KeyColumn: "ID_GVA12",
                BusinessKeySelect: "CONCAT(RTRIM(T.T_COMP), '|', RTRIM(T.N_COMP))"),

            ["CuentaCorrienteImputaciones"] = new(
                LogicalName: "CuentaCorrienteImputaciones",
                SourceTable: "GVA07",
                KeyColumn: "ID_GVA07",
                BusinessKeySelect: "CONCAT(RTRIM(T.T_COMP), '|', RTRIM(T.N_COMP))"),

            ["Vendedores"] = new(
                LogicalName: "Vendedores",
                SourceTable: "GVA23",
                KeyColumn: "ID_GVA23",
                BusinessKeySelect: "RTRIM(T.COD_VENDED)",
                ExtraFilter: "T.COD_VENDED IS NOT NULL AND T.COD_VENDED <> ''"),
        };

    private readonly string _connectionString;
    private readonly ILogger<ChangeTrackingService> _logger;

    public ChangeTrackingService(IConfiguration config, ILogger<ChangeTrackingService> logger)
    {
        _connectionString = config["SqlServerConnection"]
            ?? throw new InvalidOperationException("SqlServerConnection no configurada");
        _logger = logger;
    }

    public async Task<long?> GetCurrentVersionAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<long?>(new CommandDefinition(
            "SELECT CHANGE_TRACKING_CURRENT_VERSION()", cancellationToken: ct));
    }

    public async Task<bool> IsDatabaseChangeTrackingEnabledAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM sys.change_tracking_databases WHERE database_id = DB_ID()",
            cancellationToken: ct));
        return count > 0;
    }

    public async Task<bool> IsChangeTrackingEnabledAsync(string logicalName, CancellationToken ct)
    {
        var cfg = ResolveConfig(logicalName);
        await using var conn = new SqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(1) FROM sys.change_tracking_tables
              WHERE object_id = OBJECT_ID(@tableName)",
            new { tableName = cfg.SourceTable },
            cancellationToken: ct));
        return count > 0;
    }

    public async Task EnableDatabaseChangeTrackingAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(
            @"DECLARE @sql NVARCHAR(MAX) =
                'ALTER DATABASE [' + DB_NAME() + '] SET CHANGE_TRACKING = ON '
              + '(CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON)';
              EXEC sp_executesql @sql;",
            cancellationToken: ct));
        _logger.LogInformation("Change Tracking habilitado a nivel DB (retención 7 días)");
    }

    public async Task EnableChangeTrackingAsync(string logicalName, CancellationToken ct)
    {
        var cfg = ResolveConfig(logicalName);
        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(
            $"ALTER TABLE {cfg.SourceTable} ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF)",
            cancellationToken: ct));
        _logger.LogInformation("Change Tracking habilitado en tabla {Table}", cfg.SourceTable);
    }

    public async Task EnsureChangeTrackingEnabledAsync(string logicalName, CancellationToken ct)
    {
        if (!await IsDatabaseChangeTrackingEnabledAsync(ct))
            await EnableDatabaseChangeTrackingAsync(ct);

        if (!await IsChangeTrackingEnabledAsync(logicalName, ct))
            await EnableChangeTrackingAsync(logicalName, ct);
    }

    public async Task EnsureSyncStateTableAsync(CancellationToken ct)
    {
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'SyncState' AND type = 'U')
            BEGIN
                CREATE TABLE SyncState (
                    Tabla        VARCHAR(50) NOT NULL PRIMARY KEY,
                    LastVersion  BIGINT      NOT NULL,
                    UpdatedAt    DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END";

        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<long?> GetMinValidVersionAsync(string logicalName, CancellationToken ct)
    {
        var cfg = ResolveConfig(logicalName);
        await using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<long?>(new CommandDefinition(
            $"SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('{cfg.SourceTable}'))",
            cancellationToken: ct));
    }

    public async Task<bool> IsVersionValidAsync(string logicalName, long lastVersion, CancellationToken ct)
    {
        var min = await GetMinValidVersionAsync(logicalName, ct);
        if (min is null)
        {
            _logger.LogWarning("Change Tracking no habilitado para {Table}", logicalName);
            return false;
        }
        return lastVersion >= min.Value;
    }

    public async Task<IReadOnlyList<ChangeRecord>> GetChangesAsync(
        string logicalName, long lastVersion, CancellationToken ct)
    {
        var cfg = ResolveConfig(logicalName);

        var extra = string.IsNullOrWhiteSpace(cfg.ExtraFilter)
            ? string.Empty
            : $"AND {cfg.ExtraFilter}";

        var sql = $@"
            SELECT CT.SYS_CHANGE_VERSION AS Version,
                   CT.SYS_CHANGE_OPERATION AS Operation,
                   {cfg.BusinessKeySelect} AS ClaveValor
            FROM CHANGETABLE(CHANGES {cfg.SourceTable}, @lastVersion) AS CT
            INNER JOIN {cfg.SourceTable} T ON T.{cfg.KeyColumn} = CT.{cfg.KeyColumn}
            WHERE CT.SYS_CHANGE_OPERATION IN ('I','U')
              {extra}
            ORDER BY CT.SYS_CHANGE_VERSION;";

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<ChangeRow>(new CommandDefinition(
            sql, new { lastVersion }, cancellationToken: ct));

        var list = new List<ChangeRecord>();
        foreach (var r in rows)
        {
            if (string.IsNullOrEmpty(r.ClaveValor)) continue;
            var op = !string.IsNullOrEmpty(r.Operation) ? r.Operation[0] : '?';
            list.Add(new ChangeRecord(r.Version, op, r.ClaveValor));
        }

        _logger.LogDebug("CT {Table}: {Count} cambios desde v{Version}",
            logicalName, list.Count, lastVersion);

        return list;
    }

    private static TableSyncConfig ResolveConfig(string logicalName)
    {
        if (!Tables.TryGetValue(logicalName, out var cfg))
            throw new ArgumentException($"Tabla no registrada en whitelist: {logicalName}", nameof(logicalName));
        return cfg;
    }

    private sealed class ChangeRow
    {
        public long Version { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string? ClaveValor { get; set; }
    }
}
