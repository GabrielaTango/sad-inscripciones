using System.Data;
using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class PromocionCuponRepository : IPromocionCuponRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public PromocionCuponRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<PromocionCupon>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<PromocionCupon>(
            "SELECT * FROM PromocionCupones WHERE DeletedAt IS NULL ORDER BY CreatedAt DESC");
    }

    public async Task<PromocionCupon?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PromocionCupon>(
            "SELECT * FROM PromocionCupones WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<PromocionCupon>> GetByPromocionIdAsync(int promocionId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<PromocionCupon>(
            "SELECT * FROM PromocionCupones WHERE PromocionId = @PromocionId AND DeletedAt IS NULL ORDER BY CreatedAt DESC",
            new { PromocionId = promocionId });
    }

    public async Task<IEnumerable<PromocionCupon>> GetByDocumentoAsync(string documento)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<PromocionCupon>(
            "SELECT * FROM PromocionCupones WHERE Documento = @Documento AND DeletedAt IS NULL ORDER BY CreatedAt DESC",
            new { Documento = documento });
    }

    public async Task<PromocionCupon?> GetByCodigoAsync(string codigo)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PromocionCupon>(
            "SELECT * FROM PromocionCupones WHERE Codigo = @Codigo AND DeletedAt IS NULL",
            new { Codigo = codigo });
    }

    public async Task<int> CreateAsync(PromocionCupon entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO PromocionCupones (PromocionId, Documento, Codigo, TipoDescuento, Valor, Acumulable,
                Usado, FechaVencimiento, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@PromocionId, @Documento, @Codigo, @TipoDescuento, @Valor, @Acumulable,
                0, @FechaVencimiento, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task CreateCuponInscripcionesAsync(int promocionCuponId, IEnumerable<int> inscripcionIds)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO PromocionCuponInscripciones (PromocionCuponId, InscripcionId)
            VALUES (@PromocionCuponId, @InscripcionId)";
        var parameters = inscripcionIds.Select(id => new { PromocionCuponId = promocionCuponId, InscripcionId = id });
        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<IEnumerable<int>> GetInscripcionesDisponiblesAsync(string documento, int promocionId, DateTime desde)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            SELECT i.Id FROM Inscripciones i
            WHERE i.Documento = @Documento AND i.Estado = 'Confirmada'
              AND i.FechaInscripcion >= @Desde AND i.DeletedAt IS NULL
              AND NOT EXISTS (
                SELECT 1 FROM PromocionCuponInscripciones pci
                INNER JOIN PromocionCupones pc ON pci.PromocionCuponId = pc.Id
                WHERE pci.InscripcionId = i.Id AND pc.PromocionId = @PromocionId
              )
            ORDER BY i.FechaInscripcion ASC";
        return await connection.QueryAsync<int>(sql, new { Documento = documento, PromocionId = promocionId, Desde = desde });
    }

    public async Task<IEnumerable<PromocionCuponDisponibleDto>> GetDisponiblesByDocumentoAsync(string documento)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            SELECT pc.Id, pc.Codigo, pc.TipoDescuento, pc.Valor, pc.Acumulable, pc.FechaVencimiento,
                   p.Nombre AS PromocionNombre
            FROM PromocionCupones pc
            INNER JOIN Promociones p ON pc.PromocionId = p.Id
            WHERE pc.Documento = @Documento AND pc.Usado = 0 AND pc.DeletedAt IS NULL
              AND (pc.FechaVencimiento IS NULL OR pc.FechaVencimiento >= UTC_TIMESTAMP())
            ORDER BY pc.CreatedAt DESC";
        return await connection.QueryAsync<PromocionCuponDisponibleDto>(sql, new { Documento = documento });
    }

    public async Task<bool> MarcarUsadoAsync(string codigo, int inscripcionDestinoId, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = @"
            UPDATE PromocionCupones
            SET Usado = 1, FechaUso = UTC_TIMESTAMP(), InscripcionDestinoId = @InscripcionDestinoId, UpdatedAt = UTC_TIMESTAMP()
            WHERE Codigo = @Codigo AND Usado = 0 AND DeletedAt IS NULL";
        var rows = await connection.ExecuteAsync(sql, new { Codigo = codigo, InscripcionDestinoId = inscripcionDestinoId }, transaction);
        return rows > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE PromocionCupones SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
