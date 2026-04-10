using Dapper;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;

namespace SAD.Inscripciones.API.Repositories;

public class EventoRepository : IEventoRepository
{
    private readonly DbConnectionFactory _dbFactory;

    public EventoRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Evento>> GetAllAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryAsync<Evento>(
            "SELECT * FROM Eventos WHERE DeletedAt IS NULL ORDER BY Id DESC");
    }

    public async Task<Evento?> GetByIdAsync(int id)
    {
        using var connection = _dbFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Evento>(
            "SELECT * FROM Eventos WHERE Id = @Id AND DeletedAt IS NULL", new { Id = id });
    }

    public async Task<IEnumerable<Evento>> GetActivosAsync()
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            SELECT e.*,
              CASE WHEN EXISTS (
                  SELECT 1 FROM EventoPrecios ep
                  INNER JOIN TiposAlumno ta ON ep.TipoAlumnoId = ta.Id
                  WHERE ep.EventoId = e.Id AND ep.Activo = 1 AND ep.DeletedAt IS NULL AND ta.DeletedAt IS NULL
              ) AND NOT EXISTS (
                  SELECT 1 FROM EventoPrecios ep
                  INNER JOIN TiposAlumno ta ON ep.TipoAlumnoId = ta.Id
                  WHERE ep.EventoId = e.Id AND ep.Activo = 1 AND ep.DeletedAt IS NULL AND ta.DeletedAt IS NULL
                    AND ta.Nombre != 'Socio'
              ) THEN 1 ELSE 0 END AS SoloSocios
            FROM Eventos e
            WHERE e.Activo = 1 AND e.DeletedAt IS NULL
            ORDER BY e.Id DESC";
        return await connection.QueryAsync<Evento>(sql);
    }

    public async Task<int> CreateAsync(Evento entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            INSERT INTO Eventos (TipoEventoId, Titulo, Descripcion, FechaInicio, FechaFin, FechaCierreInscripcion, Lugar, Modalidad, MaxInscriptos, Activo, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt)
            VALUES (@TipoEventoId, @Titulo, @Descripcion, @FechaInicio, @FechaFin, @FechaCierreInscripcion, @Lugar, @Modalidad, @MaxInscriptos, @Activo, @CreatedBy, @UpdatedBy, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Evento entity)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Eventos
            SET TipoEventoId = @TipoEventoId, Titulo = @Titulo, Descripcion = @Descripcion,
                FechaInicio = @FechaInicio, FechaFin = @FechaFin, FechaCierreInscripcion = @FechaCierreInscripcion,
                Lugar = @Lugar, Modalidad = @Modalidad, MaxInscriptos = @MaxInscriptos, Activo = @Activo,
                UpdatedBy = @UpdatedBy, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, string deletedBy)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Eventos SET DeletedAt = UTC_TIMESTAMP(), UpdatedBy = @DeletedBy
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, DeletedBy = deletedBy }) > 0;
    }

    public async Task<bool> UpdateTerminosArchivoAsync(int id, string? archivo)
    {
        using var connection = _dbFactory.CreateConnection();
        const string sql = @"
            UPDATE Eventos SET TerminosArchivo = @Archivo, UpdatedAt = UTC_TIMESTAMP()
            WHERE Id = @Id AND DeletedAt IS NULL";
        return await connection.ExecuteAsync(sql, new { Id = id, Archivo = archivo }) > 0;
    }
}
