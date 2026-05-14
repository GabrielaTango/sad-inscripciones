using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAD.Inscripciones.API.Data;

namespace SAD.Inscripciones.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly DbConnectionFactory _db;

    public DashboardController(DbConnectionFactory db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        using var conn = _db.CreateConnection();
        conn.Open();

        var totalEventosActivos = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Eventos WHERE Activo = 1 AND DeletedAt IS NULL");

        var totalInscripciones = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Inscripciones WHERE DeletedAt IS NULL");

        var inscripcionesHoy = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Inscripciones WHERE DATE(FechaInscripcion) = CURDATE() AND DeletedAt IS NULL");

        var inscripcionesSemana = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Inscripciones WHERE FechaInscripcion >= DATE_SUB(NOW(), INTERVAL 7 DAY) AND DeletedAt IS NULL");

        var inscripcionesPorEstado = await conn.QueryAsync<InscripcionesPorEstadoDto>(
            "SELECT Estado, COUNT(*) AS Cantidad FROM Inscripciones WHERE DeletedAt IS NULL GROUP BY Estado");

        var inscripcionesPorEvento = await conn.QueryAsync<InscripcionesPorEventoDto>(
            @"SELECT e.Id AS EventoId, e.Titulo,
                     COUNT(i.Id) AS Total,
                     SUM(CASE WHEN i.Estado = 'Confirmada' THEN 1 ELSE 0 END) AS Confirmadas,
                     SUM(CASE WHEN i.Estado = 'Reservada' THEN 1 ELSE 0 END) AS Reservadas,
                     SUM(CASE WHEN i.Estado = 'Pendiente' THEN 1 ELSE 0 END) AS Pendientes,
                     SUM(CASE WHEN i.Estado = 'Cancelada' THEN 1 ELSE 0 END) AS Canceladas
              FROM Eventos e
              LEFT JOIN Inscripciones i ON i.EventoId = e.Id AND i.DeletedAt IS NULL
              WHERE e.Activo = 1 AND e.DeletedAt IS NULL
              GROUP BY e.Id, e.Titulo
              ORDER BY Total DESC");

        var pagosPorEstado = await conn.QueryAsync<PagosPorEstadoDto>(
            @"SELECT EstadoPago, COUNT(*) AS Cantidad, IFNULL(SUM(Monto), 0) AS Monto
              FROM Pagos WHERE DeletedAt IS NULL GROUP BY EstadoPago");

        var becasUsadas = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM BecaCodigos WHERE Usado = 1 AND DeletedAt IS NULL");

        var becasDisponibles = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM BecaCodigos WHERE Usado = 0 AND DeletedAt IS NULL");

        return Ok(new
        {
            totalEventosActivos,
            totalInscripciones,
            inscripcionesHoy,
            inscripcionesSemana,
            inscripcionesPorEstado,
            inscripcionesPorEvento,
            pagosPorEstado,
            becasUsadas,
            becasDisponibles
        });
    }

    private record InscripcionesPorEstadoDto(string Estado, long Cantidad);
    private record InscripcionesPorEventoDto(int EventoId, string Titulo, long Total, decimal Confirmadas, decimal Reservadas, decimal Pendientes, decimal Canceladas);
    private record PagosPorEstadoDto(string EstadoPago, long Cantidad, decimal Monto);
}
