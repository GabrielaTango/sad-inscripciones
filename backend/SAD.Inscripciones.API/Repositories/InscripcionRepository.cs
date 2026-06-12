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
        return await connection.QueryAsync<Inscripcion>(@"
            SELECT i.*,
                   COALESCE(ta.Extranjero, 0) AS Extranjero,
                   (SELECT SUM(p.Monto) FROM Pagos p
                     WHERE p.InscripcionId = i.Id AND p.EstadoPago = 'Confirmado'
                       AND p.Moneda = 'USD' AND p.DeletedAt IS NULL) AS MontoDolares
            FROM Inscripciones i
            LEFT JOIN TiposAlumno ta ON ta.Id = i.TipoAlumnoId
            WHERE i.DeletedAt IS NULL
            ORDER BY i.FechaInscripcion DESC");
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
        return await connection.QueryAsync<Inscripcion>(@"
            SELECT i.*,
                   COALESCE(ta.Extranjero, 0) AS Extranjero,
                   (SELECT SUM(p.Monto) FROM Pagos p
                     WHERE p.InscripcionId = i.Id AND p.EstadoPago = 'Confirmado'
                       AND p.Moneda = 'USD' AND p.DeletedAt IS NULL) AS MontoDolares
            FROM Inscripciones i
            LEFT JOIN TiposAlumno ta ON ta.Id = i.TipoAlumnoId
            WHERE i.EventoId = @EventoId AND i.DeletedAt IS NULL
            ORDER BY i.FechaInscripcion DESC",
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
                FechaNacimiento, Domicilio, CodigoPostal, Localidad, Pais, Celular, Profesion, Especialidad, Institucion, Sector, PublicRef,
                CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@EventoId, @TipoAlumnoId, @Nombre, @Apellido, @Email, @Telefono, @Documento, @Provincia,
                @PrecioBase, @DescuentoAplicado, @PrecioFinal, @PrecioFinalCuotas, @CantidadCuotas, @MontoReserva, @Estado, @Observaciones, @FechaInscripcion,
                @FechaNacimiento, @Domicilio, @CodigoPostal, @Localidad, @Pais, @Celular, @Profesion, @Especialidad, @Institucion, @Sector, @PublicRef,
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
        // Transición atómica: solo cambia (y devuelve true) si el estado es distinto al actual.
        // Bajo concurrencia (StrictMode / webhook+confirm), el lock de fila serializa los UPDATE:
        // solo el primero cambia la fila; el resto matchea 0 filas → false → el caller no repite
        // los side-effects (mail, cupones).
        const string sql = @"
            UPDATE Inscripciones SET Estado = @Estado, UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL AND Estado <> @Estado";
        return await connection.ExecuteAsync(sql, new { Id = id, Estado = estado, UpdatedBy = updatedBy }) > 0;
    }

    public async Task<int> RecalcularPreciosByEventoTipoAlumnoAsync(int eventoId, int tipoAlumnoId, decimal nuevoPrecioBase, decimal? nuevoPrecioCuotas, int nuevaCantidadCuotas, string updatedBy)
    {
        // Recalcula los precios de las inscripciones aún no cobradas (Reservada/Pendiente) cuando se cambia
        // el precio del evento para un tipo de alumno. Mantiene el mismo % de descuento que tenía cada
        // inscripción (DescuentoAplicado / PrecioBase) y lo aplica sobre el precio nuevo.
        // NO se toca MontoReserva: la reserva ya fue cobrada con el monto viejo, así que se respeta;
        // solo cambia el saldo pendiente (PrecioFinal - reserva ya pagada).
        // MySQL evalúa las asignaciones del SET de izquierda a derecha; PrecioBase se asigna AL FINAL para que
        // todas las expresiones previas usen el PrecioBase original al calcular el porcentaje de descuento.
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Inscripciones
            SET DescuentoAplicado = @NuevoPrecioBase * (CASE WHEN PrecioBase > 0 THEN DescuentoAplicado / PrecioBase ELSE 0 END),
                PrecioFinal = GREATEST(0, @NuevoPrecioBase - @NuevoPrecioBase * (CASE WHEN PrecioBase > 0 THEN DescuentoAplicado / PrecioBase ELSE 0 END)),
                PrecioFinalCuotas = CASE WHEN @NuevoPrecioCuotas IS NOT NULL AND @NuevoPrecioCuotas > 0
                    THEN GREATEST(0, @NuevoPrecioCuotas - @NuevoPrecioCuotas * (CASE WHEN PrecioBase > 0 THEN DescuentoAplicado / PrecioBase ELSE 0 END))
                    ELSE NULL END,
                CantidadCuotas = CASE WHEN @NuevoPrecioCuotas IS NOT NULL AND @NuevoPrecioCuotas > 0
                    THEN @NuevaCantidadCuotas ELSE NULL END,
                PrecioBase = @NuevoPrecioBase,
                UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE EventoId = @EventoId AND TipoAlumnoId = @TipoAlumnoId
              AND Estado IN ('Reservada', 'Pendiente')
              AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new
        {
            EventoId = eventoId,
            TipoAlumnoId = tipoAlumnoId,
            NuevoPrecioBase = nuevoPrecioBase,
            NuevoPrecioCuotas = nuevoPrecioCuotas,
            NuevaCantidadCuotas = nuevaCantidadCuotas,
            UpdatedBy = updatedBy
        });
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
