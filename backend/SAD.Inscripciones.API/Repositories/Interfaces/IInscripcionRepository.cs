using System.Data;
using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IInscripcionRepository
{
    Task<IEnumerable<Inscripcion>> GetAllAsync();
    Task<Inscripcion?> GetByIdAsync(int id);
    Task<IEnumerable<Inscripcion>> GetByEventoIdAsync(int eventoId);
    Task<int> CountByEventoIdAsync(int eventoId);
    Task<int> CountConfirmadasByDocumentoAsync(string documento, DateTime desde);
    Task<bool> ExistsActivaByEventoAndDocumentoAsync(int eventoId, string documento);
    Task<int> CreateAsync(Inscripcion entity, IDbConnection? connection = null, IDbTransaction? transaction = null);
    Task<bool> UpdateAsync(Inscripcion entity);
    Task<bool> UpdateEstadoAsync(int id, string estado, string updatedBy);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
    Task<IEnumerable<DTOs.InscripcionPendienteDto>> GetPendientesByDocumentoAsync(string documento, int? eventoId);
    Task<int> CountPendientesByDocumentoAsync(string documento);
}
