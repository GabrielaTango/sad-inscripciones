using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class PromocionRepository : IPromocionRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public PromocionRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Promocion>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Promocion>(
            "SELECT * FROM Promociones WHERE DeletedAt IS NULL ORDER BY CreatedAt DESC");
    }

    public async Task<Promocion?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Promocion>(
            "SELECT * FROM Promociones WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<Promocion>> GetActiveAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Promocion>(@"
            SELECT * FROM Promociones
            WHERE Activo = 1 AND DeletedAt IS NULL
              AND FechaVigenciaDesde <= UTC_TIMESTAMP()
              AND FechaVigenciaHasta >= UTC_TIMESTAMP()");
    }

    public async Task<int> CreateAsync(Promocion entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO Promociones (Nombre, Descripcion, TipoAlumnoId, CantidadCursosRequeridos, PeriodoMeses,
                TipoDescuento, Valor, Acumulable, FechaVigenciaDesde, FechaVigenciaHasta, DiasValidezCupon,
                Activo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@Nombre, @Descripcion, @TipoAlumnoId, @CantidadCursosRequeridos, @PeriodoMeses,
                @TipoDescuento, @Valor, @Acumulable, @FechaVigenciaDesde, @FechaVigenciaHasta, @DiasValidezCupon,
                @Activo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Promocion entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Promociones
            SET Nombre = @Nombre, Descripcion = @Descripcion, TipoAlumnoId = @TipoAlumnoId,
                CantidadCursosRequeridos = @CantidadCursosRequeridos, PeriodoMeses = @PeriodoMeses,
                TipoDescuento = @TipoDescuento, Valor = @Valor, Acumulable = @Acumulable,
                FechaVigenciaDesde = @FechaVigenciaDesde, FechaVigenciaHasta = @FechaVigenciaHasta,
                DiasValidezCupon = @DiasValidezCupon, Activo = @Activo,
                UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Promociones SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }
}
