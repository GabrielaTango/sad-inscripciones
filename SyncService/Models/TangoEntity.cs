using System.Data;
using System.Globalization;
using SyncService.Helpers;

namespace SyncService.Models;

/// <summary>
/// Base para entidades Tango que cargan defaults desde XML y generan INSERT completos.
/// Porta el patrón Delta5.GVA con Campos/Valores/Insert().
/// </summary>
public abstract class TangoEntity
{
    protected readonly Dictionary<string, string> _values = new();
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
        _values[field] = value.ToString();
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
        _values[field] = value.ToString("dd/MM/yyyy");
    }

    public string Get(string field) => _values.TryGetValue(field, out var v) ? v : "";

    /// <summary>
    /// Fields to exclude from INSERT (identity columns managed by SQL Server).
    /// Override in subclasses to list identity/auto-generated columns.
    /// </summary>
    protected virtual HashSet<string> ExcludeFromInsert => new();

    /// <summary>
    /// Generates a full INSERT INTO {Table} (col1,col2,...) VALUES (val1,val2,...)
    /// using all fields from the XML with their current values, excluding identity columns.
    /// </summary>
    public string Insert()
    {
        var fields = _values.Where(kv => !ExcludeFromInsert.Contains(kv.Key)).ToList();
        var campos = string.Join(",\n", fields.Select(kv => kv.Key));
        var valores = string.Join(",\n", fields.Select(kv => FormatValue(kv.Key, kv.Value)));
        return $"INSERT INTO {TableName} (\n{campos}\n) VALUES (\n{valores}\n)";
    }

    /// <summary>
    /// Override in subclasses to provide FK lookups or sequences for specific fields.
    /// Default: formats based on the raw string value.
    /// </summary>
    protected virtual string FormatValue(string field, string value)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return "null";

        // Detect type by attempting to parse
        if (IsDateField(field, value, out var dt))
            return SqlH.ToDateTime(dt);

        if (IsBoolField(field, value))
            return value == "1" ? "1" : "0";

        if (IsDecimalField(value, out var dec))
            return SqlH.ToDecimal(dec);

        if (IsIntField(value, out var num))
            return SqlH.ToInt(num);

        return SqlH.ToString(value);
    }

    private static bool IsDateField(string field, string value, out DateTime dt)
    {
        dt = default;
        // Only try date parsing for known date fields or values with "/" separator
        if (!value.Contains('/')) return false;
        return DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
    }

    private static bool IsBoolField(string field, string value)
    {
        return value is "0" or "1" && (
            field.StartsWith("ES_") || field.StartsWith("EXPORTA") || field.StartsWith("GENERA_") ||
            field.StartsWith("CLAUSULA") || field.StartsWith("PERMITE_") || field.StartsWith("MON_CTE") ||
            field.StartsWith("COMP_STK") || field.StartsWith("CANCELADO") || field.StartsWith("FISCAL") ||
            field.StartsWith("AFECTA_") || field.StartsWith("APLICA_") || field.StartsWith("RG_") ||
            field.StartsWith("CAL_") || field.StartsWith("RECIBE_") || field.StartsWith("AUT_DE") ||
            field.StartsWith("PUBLICA_") || field.StartsWith("AUTORIZADO_") || field.StartsWith("INHABILITADO_") ||
            field.StartsWith("REQUIERE_") || field.StartsWith("HABILITADO") || field.StartsWith("KIT_") ||
            field.StartsWith("INSUMO_") || field.StartsWith("PROMOCION") || field.StartsWith("CONCILIADO") ||
            field.StartsWith("CONC_") || field.StartsWith("VA_DIRECTO") || field.StartsWith("CERRADO") ||
            field.StartsWith("PASE") || field.StartsWith("EXTERNO") || field.StartsWith("TRANSFERENCIA_")
        );
    }

    private static bool IsDecimalField(string value, out decimal dec)
    {
        dec = 0;
        if (value.Contains('.') && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out dec))
            return true;
        return false;
    }

    private static bool IsIntField(string value, out int num)
    {
        return int.TryParse(value, out num);
    }
}
