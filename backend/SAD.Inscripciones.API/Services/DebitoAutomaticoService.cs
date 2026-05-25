using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class DebitoAutomaticoService : IDebitoAutomaticoService
{
    private static readonly HashSet<string> MarcasValidas = new(StringComparer.OrdinalIgnoreCase) { "Visa", "Master" };

    private readonly DbConnectionFactory _dbFactory;
    private readonly ICryptoService _crypto;

    public DebitoAutomaticoService(DbConnectionFactory dbFactory, ICryptoService crypto)
    {
        _dbFactory = dbFactory;
        _crypto = crypto;
    }

    public async Task<DebitoAutomaticoInfo?> GetByCuitAsync(string cuit)
    {
        using var conn = _dbFactory.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<DebitoRow>(
            @"SELECT DebitoAutomatico AS Activo, MarcaTarjeta, TarjetaUltimos4,
                     VencimientoTarjeta AS Vencimiento, FechaAltaDebito AS FechaAlta,
                     EstadoSyncDebito AS EstadoSync
              FROM Clientes WHERE Cuit = @Cuit",
            new { Cuit = cuit });

        if (row == null) return null;

        return new DebitoAutomaticoInfo
        {
            Activo = row.Activo,
            MarcaTarjeta = row.MarcaTarjeta,
            TarjetaUltimos4 = row.TarjetaUltimos4,
            Vencimiento = row.Vencimiento,
            FechaAlta = row.FechaAlta,
            EstadoSync = row.EstadoSync ?? "Sincronizado",
        };
    }

    public async Task GuardarAsync(string cuit, GuardarDebitoAutomaticoRequest request)
    {
        var marca = (request.MarcaTarjeta ?? string.Empty).Trim();
        if (!MarcasValidas.Contains(marca))
            throw new ArgumentException("Marca inválida. Solo Visa o Master.");
        marca = char.ToUpper(marca[0]) + marca[1..].ToLower();

        var numero = new string((request.NumeroTarjeta ?? string.Empty).Where(char.IsDigit).ToArray());
        if (numero.Length != 16)
            throw new ArgumentException("El número de tarjeta debe tener 16 dígitos.");
        if (!PasaLuhn(numero))
            throw new ArgumentException("El número de tarjeta no es válido.");

        var vto = (request.Vencimiento ?? string.Empty).Trim();
        if (!EsVencimientoValido(vto, out var vtoNorm))
            throw new ArgumentException("Vencimiento inválido o ya pasado. Formato MM/YY.");

        using var conn = _dbFactory.CreateConnection();

        var actual = await conn.QueryFirstOrDefaultAsync<(bool Activo, string EstadoSync)?>(
            "SELECT DebitoAutomatico AS Activo, EstadoSyncDebito AS EstadoSync FROM Clientes WHERE Cuit = @Cuit",
            new { Cuit = cuit });
        if (actual == null)
            throw new InvalidOperationException("No existe el cliente para el CUIT indicado.");
        if (actual.Value.EstadoSync != "Sincronizado")
            throw new InvalidOperationException("Hay una sincronización con Tango pendiente. Reintentá en unos minutos.");

        var cifrado = _crypto.Encrypt(numero);
        var ultimos4 = numero[^4..];
        var nuevoEstado = actual.Value.Activo ? "PendienteModificacion" : "PendienteAlta";

        await conn.ExecuteAsync(@"
            UPDATE Clientes
            SET DebitoAutomatico = 1,
                MarcaTarjeta = @Marca,
                NumeroTarjetaCifrado = @Cifrado,
                TarjetaUltimos4 = @Ultimos4,
                VencimientoTarjeta = @Vto,
                FechaAltaDebito = COALESCE(FechaAltaDebito, UTC_TIMESTAMP()),
                EstadoSyncDebito = @Estado
            WHERE Cuit = @Cuit",
            new { Cuit = cuit, Marca = marca, Cifrado = cifrado, Ultimos4 = ultimos4, Vto = vtoNorm, Estado = nuevoEstado });
    }

    public async Task DarDeBajaAsync(string cuit)
    {
        using var conn = _dbFactory.CreateConnection();

        var actual = await conn.QueryFirstOrDefaultAsync<(bool Activo, string EstadoSync)?>(
            "SELECT DebitoAutomatico AS Activo, EstadoSyncDebito AS EstadoSync FROM Clientes WHERE Cuit = @Cuit",
            new { Cuit = cuit });
        if (actual == null)
            throw new InvalidOperationException("No existe el cliente para el CUIT indicado.");
        if (!actual.Value.Activo)
            throw new ArgumentException("El socio no tiene débito automático activo.");
        if (actual.Value.EstadoSync != "Sincronizado")
            throw new InvalidOperationException("Hay una sincronización con Tango pendiente. Reintentá en unos minutos.");

        // Mantenemos los datos cifrados/últimos 4 hasta que el SyncService confirme la baja
        // en Tango. Cuando llegue el PATCH /api/sync/debitos-automaticos/{cuit}/tango, se limpian.
        await conn.ExecuteAsync(@"
            UPDATE Clientes
            SET EstadoSyncDebito = 'PendienteBaja'
            WHERE Cuit = @Cuit",
            new { Cuit = cuit });
    }

    private static bool PasaLuhn(string numero)
    {
        var sum = 0;
        var alt = false;
        for (var i = numero.Length - 1; i >= 0; i--)
        {
            var n = numero[i] - '0';
            if (alt)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alt = !alt;
        }
        return sum % 10 == 0;
    }

    private static bool EsVencimientoValido(string mmYy, out string normalizado)
    {
        normalizado = string.Empty;
        var partes = mmYy.Split('/');
        if (partes.Length != 2) return false;
        if (!int.TryParse(partes[0], out var mm) || mm < 1 || mm > 12) return false;
        if (!int.TryParse(partes[1], out var yy) || yy < 0 || yy > 99) return false;

        var year = 2000 + yy;
        // Considera el último día del mes como vencimiento real.
        var ultimoDia = new DateTime(year, mm, DateTime.DaysInMonth(year, mm));
        if (ultimoDia < DateTime.UtcNow.Date) return false;

        normalizado = $"{mm:D2}/{yy:D2}";
        return true;
    }

    private sealed class DebitoRow
    {
        public bool Activo { get; set; }
        public string? MarcaTarjeta { get; set; }
        public string? TarjetaUltimos4 { get; set; }
        public string? Vencimiento { get; set; }
        public DateTime? FechaAlta { get; set; }
        public string? EstadoSync { get; set; }
    }
}
