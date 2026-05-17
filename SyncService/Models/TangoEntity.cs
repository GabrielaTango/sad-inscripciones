using System.Data;
using System.Globalization;
using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>
/// Base para entidades Tango que cargan defaults desde XML y generan INSERT completos.
/// La conversión a SQL se hace según el tipo real de cada columna en SQL Server
/// (vía TangoSchemaCache, precargado al startup del Worker).
/// </summary>
public abstract class TangoEntity
{
    protected readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    protected static readonly CultureInfo Culture = new("en-US");

    protected abstract string TableName { get; }
    protected abstract string XmlFileName { get; }

    protected TangoEntity()
    {
        LoadDefaults();
    }

    private void LoadDefaults()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", XmlFileName);
        if (!File.Exists(path)) return;

        var ds = new DataSet();
        ds.ReadXml(path);
        if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0) return;

        var row = ds.Tables[0].Rows[0];
        foreach (DataColumn col in ds.Tables[0].Columns)
        {
            _values[col.ColumnName] = row[col.ColumnName]?.ToString() ?? "";
        }
    }

    /// <summary>Sets a string value.</summary>
    public void Set(string field, string? value)
    {
        _values[field] = value ?? "";
    }

    /// <summary>Sets an integer value.</summary>
    public void SetInt(string field, int value)
    {
        _values[field] = value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Sets a decimal value.</summary>
    public void SetDecimal(string field, decimal value)
    {
        _values[field] = value.ToString("0.0000000", CultureInfo.InvariantCulture);
    }

    /// <summary>Sets a boolean value (stored as "0"/"1").</summary>
    public void SetBool(string field, bool value)
    {
        _values[field] = value ? "1" : "0";
    }

    /// <summary>Sets a DateTime value (stored as "dd/MM/yyyy").</summary>
    public void SetDate(string field, DateTime value)
    {
        _values[field] = value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    public string Get(string field) => _values.TryGetValue(field, out var v) ? v : "";

    /// <summary>
    /// Fields to exclude from INSERT (identity columns managed by SQL Server).
    /// Override in subclasses to list identity/auto-generated columns.
    /// </summary>
    protected virtual HashSet<string> ExcludeFromInsert => new();

    /// <summary>
    /// Generates INSERT INTO {Table} (col,...) VALUES (val,...) usando solo las columnas
    /// que existen en la tabla real (intersección entre _values y schema cacheado).
    /// </summary>
    public string Insert()
    {
        if (!TangoSchemaCache.IsLoaded(TableName))
            throw new InvalidOperationException(
                $"Schema de tabla {TableName} no precargado en TangoSchemaCache. Asegurar que esté en TablasInsertables y LoadAllAsync se haya ejecutado.");

        var fields = _values
            .Where(kv => !ExcludeFromInsert.Contains(kv.Key))
            .Where(kv => TangoSchemaCache.HasColumn(TableName, kv.Key))
            .ToList();

        if (fields.Count == 0)
            throw new InvalidOperationException($"Sin campos para insertar en {TableName}");

        var campos = string.Join(",\n", fields.Select(kv => kv.Key));
        var valores = string.Join(",\n", fields.Select(kv => FormatValue(kv.Key, kv.Value)));
        return $"INSERT INTO {TableName} (\n{campos}\n) VALUES (\n{valores}\n)";
    }

    /// <summary>
    /// Override en subclases para mapear campos especiales (FK con subquery, sequences, etc.).
    /// El default usa el tipo SQL real de la columna desde el cache.
    /// </summary>
    protected virtual string FormatValue(string field, string value)
    {
        if (!TangoSchemaCache.TryGetType(TableName, field, out var sqlType))
            throw new InvalidOperationException(
                $"Columna {TableName}.{field} no existe en INFORMATION_SCHEMA. " +
                $"Revisar XML default o agregar la tabla a TablasInsertables.");

        // Para columnas char/varchar/nchar/nvarchar truncamos al ancho real para evitar 8152.
        if (IsBoundedStringType(sqlType)
            && TangoSchemaCache.TryGetMaxLength(TableName, field, out var maxLen)
            && value.Length > maxLen)
        {
            Serilog.Log.Warning(
                "Truncando {Table}.{Field}: {Original} chars → {Max} (valor=\"{Value}\")",
                TableName, field, value.Length, maxLen, value);
            value = value.Substring(0, maxLen);
        }

        return FormatBySqlType(sqlType, value);
    }

    private static bool IsBoundedStringType(string sqlType) =>
        sqlType is "char" or "nchar" or "varchar" or "nvarchar";

    /// <summary>Formatea un valor según el DATA_TYPE de SQL Server.</summary>
    protected static string FormatBySqlType(string sqlType, string value)
    {
        // null/vacío → NULL para todos los tipos.
        //if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
        //    return "null";

        return sqlType switch
        {
            "int" or "smallint" or "tinyint" =>
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                    ? (i == -1 ? "null" : i.ToString(CultureInfo.InvariantCulture))
                    : "null",
            "bigint" =>
                long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                    ? (l == -1L ? "null" : l.ToString(CultureInfo.InvariantCulture))
                    : "null",

            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" =>
                ParseDecimal(value, out var d) ? (d == -1m ? "null" : SqlH.ToDecimal(d)) : "null",

            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" =>
                ParseDate(value, out var dt) ? SqlH.ToDateTime(dt) : "null",

            "bit" => value.Trim() switch { "1" or "true" or "S" or "s" => "1", _ => "0" },

            // Strings — preserva ceros a la izquierda, padding, etc.
            "char" or "nchar" or "varchar" or "nvarchar" or "text" or "ntext" =>
                SqlH.ToString(value),

            "uniqueidentifier" => $"'{value}'",

            _ => throw new InvalidOperationException(
                $"Tipo SQL no soportado: {sqlType} (valor='{value}')"),
        };
    }

    private static bool ParseDecimal(string value, out decimal d)
    {
        var normalized = value.Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
    }

    private static bool ParseDate(string value, out DateTime dt)
    {
        if (DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return true;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
    }
}
