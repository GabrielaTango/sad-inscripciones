using System.Data;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IPromocionCuponRepository
{
    Task<IEnumerable<PromocionCupon>> GetAllAsync();
    Task<PromocionCupon?> GetByIdAsync(int id);
    Task<IEnumerable<PromocionCupon>> GetByPromocionIdAsync(int promocionId);
    Task<IEnumerable<PromocionCupon>> GetByDocumentoAsync(string documento);
    Task<PromocionCupon?> GetByCodigoAsync(string codigo);
    Task<IEnumerable<PromocionCuponDisponibleDto>> GetDisponiblesByDocumentoAsync(string documento);
    Task<int> CreateAsync(PromocionCupon entity);
    Task CreateCuponInscripcionesAsync(int promocionCuponId, IEnumerable<int> inscripcionIds);
    Task<IEnumerable<int>> GetInscripcionesDisponiblesAsync(string documento, int promocionId, DateTime desde);
    Task<bool> MarcarUsadoAsync(string codigo, int inscripcionDestinoId, IDbConnection connection, IDbTransaction transaction);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
}
