using System.Globalization;

namespace SyncService.Helpers;

/// <summary>
/// Port of Comunes.SQLH — SQL value formatting helpers for Tango database inserts.
/// All methods generate raw SQL fragments (not parameterized) matching Tango's expected format.
/// </summary>
public static class SqlH
{
    public static string ToDateTime(DateTime dt)
    {
        if (dt.Year > 1)
            return $"CONVERT(DATETIME,'{dt:dd/MM/yyyy}',103)";
        return "null";
    }

    public static string ToInt(int value)
    {
        return value >= 0 ? value.ToString() : "null";
    }

    public static string ToDecimal(decimal value)
    {
        return value >= 0 ? value.ToString("0.00", CultureInfo.InvariantCulture) : "null";
    }

    public static string ToString(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return "null";
        return $"'{value}'";
    }

    public static string ToBool(bool value) => value ? "1" : "0";

    public static string ToLong(long value) => value >= 0 ? value.ToString() : "null";

    public static string ToSequence(string tabla) => $"NEXT VALUE FOR SEQUENCE_{tabla}";

    /// <summary>Subquery: (SELECT ID_{tabla} FROM {tabla} WHERE {campo} = '{valor}')</summary>
    public static string TraerId(string tabla, string campo, string valor)
        => $"(SELECT ID_{tabla} FROM {tabla} WHERE {campo} = '{valor}')";

    /// <summary>Subquery: (SELECT {campoFrom} FROM {tabla} WHERE {campo} = '{valor}')</summary>
    public static string TraerIdCampo(string tabla, string campoFrom, string campo, string valor)
        => $"(SELECT {campoFrom} FROM {tabla} WHERE {campo} = '{valor}')";

    /// <summary>Subquery with non-quoted value: (SELECT {campoFrom} FROM {tabla} WHERE {campo} = {valor})</summary>
    public static string TraerIdCampoSubConsulta(string tabla, string campoFrom, string campo, string valor)
        => $"(SELECT {campoFrom} FROM {tabla} WHERE {campo} = {valor})";

    // --- Document Number Encryption/Decryption (GVA43 talonario) ---

    private static readonly string[][] EncryptTables =
    [
        ["74","75","76","77","70","71","72","73","7C","7D"],
        ["07","06","05","04","03","02","01","00","0F","0E"],
        ["43","42","41","40","47","46","45","44","4B","4A"],
        ["7B","7A","79","78","7F","7E","7D","7C","73","72"],
        ["51","50","53","52","55","54","57","56","59","58"],
        ["7E","7F","7C","7D","7A","7B","78","79","76","77"],
        ["07","06","05","04","03","02","01","00","0F","0E"],
        ["65","64","67","66","61","60","63","62","6D","6C"],
    ];

    public static string DocNumberEncrypt(string number)
    {
        var padded = number.PadLeft(8, '0');
        var result = "";
        for (int i = 0; i < 8; i++)
        {
            int digit = int.Parse(padded.Substring(i, 1));
            result += EncryptTables[i][digit];
        }
        return result;
    }

    public static string DocNumberDecrypt(string encrypted)
    {
        var padded = encrypted.PadLeft(16, '0');
        var result = "";
        int pos = 0;
        for (int i = 0; i < padded.Length; i += 2)
        {
            string code = padded.Substring(i, 2);
            int digit = Array.IndexOf(EncryptTables[pos], code);
            result += digit >= 0 ? digit.ToString() : "0";
            pos++;
        }
        return result;
    }
}
