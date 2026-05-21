using System.Data;
using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class InscripcionRepository : IInscripcionRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public InscripcionRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Inscripcion>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Inscripcion>(
            "SELECT * FROM Inscripciones WHERE DeletedAt IS NULL ORDER BY FechaInscripcion DESC");
    }

    public async Task<Inscripcion?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Inscripcion>(
            "SELECT * FROM Inscripciones WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<Inscripcion>> GetByEventoIdAsync(int eventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Inscripcion>(
            "SELECT * FROM Inscripciones WHERE EventoId = @EventoId AND DeletedAt IS NULL ORDER BY FechaInscripcion DESC",
            new { EventoId = eventoId });
    }

    public async Task<int> CountByEventoIdAsync(int eventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Inscripciones WHERE EventoId = @EventoId AND DeletedAt IS NULL",
            new { EventoId = eventoId });
    }

    public async Task<int> CountConfirmadasByDocumentoAsync(string documento, DateTime desde)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM Inscripciones
              WHERE Documento = @Documento AND Estado = 'Confirmada' AND FechaInscripcion >= @Desde AND DeletedAt IS NULL",
            new { Documento = documento, Desde = desde });
    }

    public async Task<bool> ExistsActivaByEventoAndDocumentoAsync(int eventoId, string documento)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(*) FROM Inscripciones
            WHERE EventoId = @EventoId
              AND Documento = @Documento
              AND Estado IN ('Pendiente', 'Reservada', 'Confirmada')
              AND DeletedAt IS NULL";
        return await connection.ExecuteScalarAsync<int>(sql, new { EventoId = eventoId, Documento = documento }) > 0;
    }

    public async Task<int> CreateAsync(Inscripcion entity, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = @"
            INSERT INTO Inscripciones (EventoId, TipoAlumnoId, Nombre, Apellido, Email, Telefono, Documento, Provincia,
                PrecioBase, DescuentoAplicado, PrecioFinal, PrecioFinalCuotas, CantidadCuotas, MontoReserva, Estado, Observaciones, FechaInscripcion,
                FechaNacimiento, Domicilio, CodigoPostal, Localidad, Pais, Celular, Profesion, Especialidad, Institucion, Sector,
                CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@EventoId, @TipoAlumnoId, @Nombre, @Apellido, @Email, @Telefono, @Documento, @Provincia,
                @PrecioBase, @DescuentoAplicado, @PrecioFinal, @PrecioFinalCuotas, @CantidadCuotas, @MontoReserva, @Estado, @Observaciones, @FechaInscripcion,
                @FechaNacimiento, @Domicilio, @CodigoPostal, @Localidad, @Pais, @Celular, @Profesion, @Especialidad, @Institucion, @Sector,
                @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        if (connection != null)
        {
            return await connection.ExecuteScalarAsync<int>(sql, entity, transaction);
        }

        using var conn = _dbFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Inscripcion entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Inscripciones
            SET EventoId = @EventoId, TipoAlumnoId = @TipoAlumnoId, Nombre = @Nombre, Apellido = @Apellido,
                Email = @Email, Telefono = @Telefono, Documento = @Documento, Provincia = @Provincia,
                PrecioBase = @PrecioBase, DescuentoAplicado = @DescuentoAplicado, PrecioFinal = @PrecioFinal,
                Estado = @Estado, Observaciones = @Observaciones,
                FechaNacimiento = @FechaNacimiento, Domicilio = @Domicilio, CodigoPostal = @CodigoPostal,
                Localidad = @Localidad, Pais = @Pais, Celular = @Celular, Profesion = @Profesion,
                Especialidad = @Especialidad, Institucion = @Institucion, Sector = @Sector,
                UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> UpdateEstadoAsync(int id, string estado, string updatedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Inscripciones SET Estado = @Estado, UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, Estado = estado, UpdatedBy = updatedBy }) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Inscripciones SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }

    public async Task<IEnumerable<DTOs.InscripcionPendienteDto>> GetPendientesByDocumentoAsync(string documento, int? eventoId)
    {
        using var connection = _dbFactory.CreateConnection();
        var sql = @"
            SELECT i.Id, i.EventoId, e.Titulo AS EventoTitulo, i.Nombre, i.Apellido, i.Email, i.Documento,
                   i.PrecioBase, i.DescuentoAplicado, i.PrecioFinal, i.PrecioFinalCuotas, i.CantidadCuotas,
                   i.MontoReserva, i.Estado, i.FechaInscripcion,
                   e.FechaInicio AS EventoFechaInicio, e.Modalidad AS EventoModalidad
            FROM Inscripciones i
            INNER JOIN Eventos e ON i.EventoId = e.Id
            WHERE i.Documento = @Documento AND i.Estado IN ('Pendiente', 'Confirmada', 'Reservada') AND i.DeletedAt IS NULL";

        if (eventoId.HasValue)
            sql += " AND i.EventoId = @EventoId";

        sql += " ORDER BY i.FechaInscripcion DESC";

        return await connection.QueryAsync<DTOs.InscripcionPendienteDto>(sql, new { Documento = documento, EventoId = eventoId });
    }

    public async Task<int> CountPendientesByDocumentoAsync(string documento)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            SELECT COUNT(*)
            FROM Inscripciones
            WHERE Documento = @Documento
              AND Estado = 'Pendiente'
              AND DeletedAt IS NULL";
        return await connection.ExecuteScalarAsync<int>(sql, new { Documento = documento });
    }
}
