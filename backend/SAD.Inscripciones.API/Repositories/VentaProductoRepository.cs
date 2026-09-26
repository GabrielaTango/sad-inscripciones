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

    public async Task<IEnumerable<VentaProducto>> ListAsync(int? productoId, string? estado)
    {
        using var connection = _dbFactory.CreateConnection();
        var sql = @"SELECT * FROM VentasProducto WHERE 1 = 1";
        if (productoId.HasValue)
        {
            sql += " AND ProductoId = @ProductoId";
        }
        if (!string.IsNullOrWhiteSpace(estado))
        {
            sql += " AND Estado = @Estado";
        }
        sql += " ORDER BY Id DESC";

        return await connection.QueryAsync<VentaProducto>(sql, new { ProductoId = productoId, Estado = estado });
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

            if (venta.Estado != "Pendiente")
            {
                // Already resolved (Pagada by a previous webhook call, or Rechazada):
                // nothing to do, keep the webhook idempotent.
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
                WHERE Id = @Id AND Estado = 'Pendiente'";
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

    public async Task<bool> MarcarRechazadaAsync(int ventaId)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE VentasProducto SET Estado = 'Rechazada', UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND Estado = 'Pendiente'";
        return await connection.ExecuteAsync(sql, new { Id = ventaId }) > 0;
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
