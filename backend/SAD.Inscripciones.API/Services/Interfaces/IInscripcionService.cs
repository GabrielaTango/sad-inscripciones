using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IInscripcionService
{
    Task<IEnumerable<Inscripcion>> GetAllAsync();
    Task<Inscripcion> GetByIdAsync(int id);
    Task<IEnumerable<Inscripcion>> GetByEventoIdAsync(int eventoId);
    Task<Inscripcion> CrearInscripcionAsync(InscripcionCreateDto dto, string createdBy);
    Task UpdateEstadoAsync(int id, string estado, string updatedBy);
    /// <summary>
    /// Fija (si todavía no lo tenía) el monto de reserva de una inscripción pendiente y lo
    /// devuelve. Es el importe que se cobra al reservar la vacante.
    /// </summary>
    Task<decimal> EstablecerMontoReservaAsync(int id, string updatedBy);
    Task DeleteAsync(int id, string deletedBy);
    Task<IEnumerable<DTOs.InscripcionPendienteDto>> GetPendientesByDocumentoAsync(string documento, int? eventoId);
    Task<int> CountPendientesByDocumentoAsync(string documento);
    Task<byte[]> ExportToExcelAsync(int? eventoId, string? estado = null);
}
