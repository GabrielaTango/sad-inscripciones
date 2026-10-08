using Dapper;
using MySqlConnector;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class VentaProductoRepository : IVentaProductoRepository
{
    private const int MySqlDuplicateEntryErrorCode = 1062;

    private readonly DbConnectionFactory _dbFactory;

    public VentaProductoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> CreatePendienteAsync(VentaProducto entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO VentasProducto (ProductoId, PublicRef, Dni, Nombre, Apellido, Email, DatosExtra, Importe, Estado, FechaAlta, CreatedAt, UpdatedAt)
            VALUES (@ProductoId, @PublicRef, @Dni, @Nombre, @Apellido, @Email, @DatosExtra, @Importe, 'Pendiente', UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<VentaProducto?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<VentaProducto>(
            "SELECT * FROM VentasProducto WHERE Id = @Id", new { Id = id });
    }

    public async Task<VentaProducto?> GetByPublicRefAsync(string publicRef)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<VentaProducto>(
            "SELECT * FROM VentasProducto WHERE PublicRef = @PublicRef", new { PublicRef = publicRef });
    }

    public async Task<IEnumerable<VentaProductoAdminRow>> ListAsync(int? productoId, string? estado, DateTime? desde, DateTime? hasta, string? texto)
    {
        using var connection = _dbFactory.CreateConnection();
        var sql = @"
            SELECT v.Id, v.ProductoId, p.Nombre AS ProductoNombre, v.Dni, v.Nombre, v.Apellido, v.Email,
                   v.DatosExtra, v.Importe, v.Estado, v.MpPaymentId, v.FechaAlta, v.FechaPago, v.MailEnviado, v.UpdatedAt
            FROM VentasProducto v
            JOIN Productos p ON p.Id = v.ProductoId
            WHERE 1 = 1";

        if (productoId.HasValue)
        {
            sql += " AND v.ProductoId = @ProductoId";
        }
        if (!string.IsNullOrWhiteSpace(estado))
        {
            sql += " AND v.Estado = @Estado";
        }
        if (desde.HasValue)
        {
            sql += " AND COALESCE(v.FechaPago, v.FechaAlta) >= @Desde";
        }
        if (hasta.HasValue)
        {
            // Limite exclusivo del dia siguiente para que "hasta" incluya el dia completo.
            sql += " AND COALESCE(v.FechaPago, v.FechaAlta) < @HastaExclusiva";
        }
        if (!string.IsNullOrWhiteSpace(texto))
        {
            sql += " AND (v.Dni LIKE @Texto OR v.Nombre LIKE @Texto OR v.Apellido LIKE @Texto OR v.Email LIKE @Texto)";
        }
        sql += " ORDER BY COALESCE(v.FechaPago, v.FechaAlta) DESC, v.Id DESC";

        return await connection.QueryAsync<VentaProductoAdminRow>(sql, new
        {
            ProductoId = productoId,
            Estado = estado,
            Desde = desde,
            HastaExclusiva = hasta?.Date.AddDays(1),
            Texto = $"%{texto}%",
        });
    }

    public async Task<ConfirmarPagoResult> ConfirmarPagoAsync(int ventaId, long mpPaymentId, decimal montoAcreditado)
    {
        using var connection = (MySqlConnection)_dbFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var venta = await connection.QueryFirstOrDefaultAsync<VentaProducto>(
                "SELECT * FROM VentasProducto WHERE Id = @Id FOR UPDATE",
                new { Id = ventaId }, transaction);

            if (venta is null)
            {
                await transaction.RollbackAsync();
                return ConfirmarPagoResult.NotFound;
            }

            if (venta.Estado != "Pendiente" && venta.Estado != "Impaga")
            {
                // Already Pagada by a previous call: nothing to do, keep the
                // webhook / verification idempotent.
                await transaction.RollbackAsync();
                return ConfirmarPagoResult.AlreadyPaid;
            }

            if (venta.Importe != montoAcreditado)
            {
                // Leave as Pendiente so the mismatch can be investigated instead of
                // silently confirming or rejecting a sale with the wrong amount.
                await transaction.RollbackAsync();
                return ConfirmarPagoResult.AmountMismatch;
            }

            const string sql = @"
                UPDATE VentasProducto
                SET Estado = 'Pagada', MpPaymentId = @MpPaymentId, FechaPago = UTC_TIMESTAMP(), UpdatedAt = UTC_TIMESTAMP()
                WHERE Id = @Id AND Estado IN ('Pendiente', 'Impaga')";
            var rows = await connection.ExecuteAsync(sql, new { Id = ventaId, MpPaymentId = mpPaymentId }, transaction);

            if (rows == 0)
            {
                // Lost the race against another concurrent confirmation of the same row.
                await transaction.RollbackAsync();
                return ConfirmarPagoResult.AlreadyPaid;
            }

            await transaction.CommitAsync();
            return ConfirmarPagoResult.Confirmed;
        }
        catch (MySqlException ex) when (ex.Number == MySqlDuplicateEntryErrorCode)
        {
            // MpPaymentId is UNIQUE: this payment id was already used to confirm
            // a different (or the same, in a race) VentaProducto row.
            await transaction.RollbackAsync();
            return ConfirmarPagoResult.AlreadyPaid;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<VentaProducto>> ListPendientesAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<VentaProducto>(
            "SELECT * FROM VentasProducto WHERE Estado = 'Pendiente' ORDER BY Id");
    }

    public async Task<bool> MarcarImpagaAsync(int ventaId, int horasMinimas)
    {
        using var connection = _dbFactory.CreateConnection();
        // FechaAlta se escribe con UTC_TIMESTAMP(), asi que la antiguedad se compara
        // contra el mismo reloj dentro de SQL. Solo transiciona desde Pendiente.
        const string sql = @"
            UPDATE VentasProducto SET Estado = 'Impaga', UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND Estado = 'Pendiente'
              AND FechaAlta < DATE_SUB(UTC_TIMESTAMP(), INTERVAL @Horas HOUR)";
        return await connection.ExecuteAsync(sql, new { Id = ventaId, Horas = horasMinimas }) > 0;
    }

    public async Task<bool> MarcarMailEnviadoAsync(int ventaId)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE VentasProducto SET MailEnviado = 1, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id";
        return await connection.ExecuteAsync(sql, new { Id = ventaId }) > 0;
    }
}
