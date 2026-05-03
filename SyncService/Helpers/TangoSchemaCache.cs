using System.Collections.Concurrent;
using Dapper;
using Microsoft.Data.SqlClient;

namespace SyncService.Helpers;

/// <summary>
/// Cache estático de columnas/tipos por tabla del SQL Server de Tango.
/// Lo poblamos eager al inicio del Worker para las tablas que insertamos
/// y luego TangoEntity.FormatValue lo consulta sincrónicamente.
/// </summary>
public static class TangoSchemaCache
{
    // tabla (case-insensitive) -> columna (case-insensitive) -> sqlType (lowercase)
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _byTable
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Tablas Tango en las que el SyncService genera INSERTs.</summary>
    public static readonly IReadOnlyList<string> TablasInsertables =
    [
        "GVA12", "GVA14", "GVA07", "GVA03", "GVA21",
        "SBA04", "SBA05",
        "DIRECCION_ENTREGA",
        "ASIENTO_COMPROBANTE_SB",
    ];

    /// <summary>Carga el schema (column_name, data_type) de una tabla y lo cachea.</summary>
    public static async Task LoadAsync(SqlConnection conn, string tableName, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COLUMN_NAME, DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName";

        var rows = await conn.QueryAsync<(string ColumnName, string DataType)>(
            new CommandDefinition(sql, new { TableName = tableName }, cancellationToken: ct));

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (col, type) in rows)
        {
            if (string.IsNullOrEmpty(col) || string.IsNullOrEmpty(type)) continue;
            dict[col] = type.Trim().ToLowerInvariant();
        }

        if (dict.Count == 0)
            throw new InvalidOperationException($"Tabla {tableName} no encontrada en INFORMATION_SCHEMA.COLUMNS");

        _byTable[tableName] = dict;
    }

    public static async Task LoadAllAsync(SqlConnection conn, CancellationToken ct = default)
    {
        foreach (var t in TablasInsertables)
            await LoadAsync(conn, t, ct);
    }

    public static bool TryGetType(string tableName, string columnName, out string sqlType)
    {
        sqlType = string.Empty;
        if (!_byTable.TryGetValue(tableName, out var cols)) return false;
        if (!cols.TryGetValue(columnName, out var t)) return false;
        sqlType = t;
        return true;
    }

    /// <summary>Devuelve true si la tabla está cargada en el cache.</summary>
    public static bool IsLoaded(string tableName) => _byTable.ContainsKey(tableName);

    /// <summary>Devuelve true si la columna existe en la tabla (según el schema cargado).</summary>
    public static bool HasColumn(string tableName, string columnName)
        => _byTable.TryGetValue(tableName, out var cols) && cols.ContainsKey(columnName);
}
