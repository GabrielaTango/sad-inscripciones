using Dapper;
using Microsoft.Data.SqlClient;
using SyncService.Models;

namespace SyncService.Services;

/// <summary>
/// Aplica el alta/modificación/baja del débito automático en GVA14, escribiendo
/// los campos CA_1096_* tanto en el XML CAMPOS_ADICIONALES como en la columna física.
/// Es el patrón provisto por el cliente, parametrizado por CUIT y campo.
/// </summary>
public class TangoDebitoAutomaticoService
{
    private readonly ILogger<TangoDebitoAutomaticoService> _logger;

    public TangoDebitoAutomaticoService(ILogger<TangoDebitoAutomaticoService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcesarAsync(SqlConnection conn, DebitoAutomaticoSyncDto dto)
    {
        var cuit = (dto.Cuit ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(cuit))
        {
            _logger.LogWarning("Débito sync sin CUIT, ignorado");
            return false;
        }

        var existeCliente = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GVA14 WHERE CUIT = @Cuit", new { Cuit = cuit });
        if (existeCliente == 0)
        {
            _logger.LogWarning("CUIT {Cuit} no existe en GVA14, se omite débito", cuit);
            return false;
        }

        Dictionary<string, string?> valores;
        if (dto.EstadoSync == "PendienteBaja")
        {
            valores = new Dictionary<string, string?>
            {
                ["CA_1096_DEBITO_AUTOMATICO"] = "N",
                ["CA_1096_TARJETA"] = string.Empty,
                ["CA_1096_NRO_TARJETA"] = string.Empty,
                ["CA_1096_VENC_TARJETA"] = string.Empty,
            };
        }
        else
        {
            valores = new Dictionary<string, string?>
            {
                ["CA_1096_DEBITO_AUTOMATICO"] = "S",
                ["CA_1096_TARJETA"] = MapearMarca(dto.MarcaTarjeta),
                ["CA_1096_NRO_TARJETA"] = (dto.NumeroTarjeta ?? string.Empty).Trim(),
                ["CA_1096_VENC_TARJETA"] = ConvertirVencimiento(dto.Vencimiento),
            };
        }

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();
        try
        {
            foreach (var (campo, valor) in valores)
                await SetCampoAsync(conn, tx, cuit, campo, valor);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        _logger.LogInformation("Débito {Estado} aplicado a Tango para CUIT {Cuit}", dto.EstadoSync, cuit);
        return true;
    }

    /// <summary>
    /// Mapea "Visa"/"Master" (como lo guarda MySQL) al valor exacto del schema
    /// de Tango para CA_1096_TARJETA — enum: "", "Sin asignar", "MASTER", "VISA".
    /// </summary>
    private static string MapearMarca(string? marca)
    {
        var m = (marca ?? string.Empty).Trim().ToUpperInvariant();
        return m switch
        {
            "VISA" => "VISA",
            "MASTER" => "MASTER",
            "MASTERCARD" => "MASTER",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Convierte "MM/YY" (como lo guarda MySQL) al formato xs:dateTime que pide
    /// el schema de CA_1096_VENC_TARJETA: "YYYY-MM-DDTHH:mm:ss" del último día
    /// del mes (ej. 05/26 → 2026-05-31T23:59:59). Si el input es inválido,
    /// devuelve "" porque el schema acepta string vacío como alternativa.
    /// </summary>
    private static string ConvertirVencimiento(string? mmYy)
    {
        if (string.IsNullOrWhiteSpace(mmYy)) return string.Empty;
        var partes = mmYy.Split('/');
        if (partes.Length != 2) return string.Empty;
        if (!int.TryParse(partes[0], out var mm) || mm < 1 || mm > 12) return string.Empty;
        if (!int.TryParse(partes[1], out var yy) || yy < 0 || yy > 99) return string.Empty;
        var year = 2000 + yy;
        var ultimoDia = DateTime.DaysInMonth(year, mm);
        return $"{year:D4}-{mm:D2}-{ultimoDia:D2}T23:59:59";
    }

    /// <summary>
    /// Actualiza un nodo de CAMPOS_ADICIONALES (XML) y, si existe la columna
    /// física homónima en GVA14, también la actualiza.
    ///
    /// Usa XML.modify(): primero asegura el root `<CAMPOS_ADICIONALES />`, después
    /// borra TODOS los nodos con el nombre dado (limpia duplicados que el patrón
    /// con LIKE/REPLACE podía dejar en versiones previas) y finalmente inserta
    /// uno con el valor nuevo. Idempotente.
    ///
    /// El nombre del nodo se interpola en el SQL dinámico — lo restringimos a
    /// `[A-Z_]+` para evitar inyección.
    /// </summary>
    private static async Task SetCampoAsync(SqlConnection conn, SqlTransaction tx, string cuit, string campo, string? valor)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(campo, "^[A-Z][A-Z0-9_]*$"))
            throw new ArgumentException($"Nombre de campo inválido: {campo}", nameof(campo));

        // 1. Asegurar root CAMPOS_ADICIONALES.
        await conn.ExecuteAsync(@"
            UPDATE GVA14
            SET CAMPOS_ADICIONALES = CONVERT(XML, '<CAMPOS_ADICIONALES />')
            WHERE CUIT = @Cuit
              AND (CAMPOS_ADICIONALES IS NULL
                   OR LTRIM(RTRIM(CONVERT(NVARCHAR(MAX), CAMPOS_ADICIONALES))) = '')",
            new { Cuit = cuit }, tx);

        // 2. Borrar todas las apariciones del nodo (sin importar forma — soporta
        //    <X></X>, <X/>, <X />, y limpia duplicados heredados).
        var deleteSql = $@"
            UPDATE GVA14
            SET CAMPOS_ADICIONALES.modify('delete /CAMPOS_ADICIONALES/{campo}')
            WHERE CUIT = @Cuit
              AND CAMPOS_ADICIONALES.exist('/CAMPOS_ADICIONALES/{campo}') = 1";
        await conn.ExecuteAsync(deleteSql, new { Cuit = cuit }, tx);

        // 3. Insertar el nodo con el nuevo valor.
        //    sql:variable("@Valor") inyecta el valor textual seguro.
        var insertSql = $@"
            UPDATE GVA14
            SET CAMPOS_ADICIONALES.modify('
                insert <{campo}>{{sql:variable(""@Valor"")}}</{campo}>
                as last into (/CAMPOS_ADICIONALES)[1]
            ')
            WHERE CUIT = @Cuit";
        await conn.ExecuteAsync(insertSql, new { Cuit = cuit, Valor = valor ?? string.Empty }, tx);

        // 4. Si la columna física homónima existe en GVA14, la actualizamos también.
        const string updateColSql = @"
            IF COL_LENGTH('GVA14', @Campo) IS NOT NULL
            BEGIN
                DECLARE @SQL NVARCHAR(MAX) = '
                    UPDATE GVA14
                    SET ' + QUOTENAME(@Campo) + ' = @Valor
                    WHERE CUIT = @Cuit
                ';
                EXEC sp_executesql @SQL, N'@Valor VARCHAR(100), @Cuit VARCHAR(50)', @Valor, @Cuit;
            END";
        await conn.ExecuteAsync(updateColSql, new { Cuit = cuit, Campo = campo, Valor = valor }, tx);
    }
}
