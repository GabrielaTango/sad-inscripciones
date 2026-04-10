using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IPromocionCuponService
{
    Task<IEnumerable<PromocionCupon>> GetAllAsync();
    Task<PromocionCupon> GetByIdAsync(int id);
    Task<IEnumerable<PromocionCupon>> GetByPromocionIdAsync(int promocionId);
    Task<IEnumerable<PromocionCupon>> GetByDocumentoAsync(string documento);
    Task<PromocionCupon?> GetByCodigoAsync(string codigo);
    Task<IEnumerable<PromocionCuponDisponibleDto>> GetDisponiblesByDocumentoAsync(string documento);
    Task GenerarCuponesParaInscripcionAsync(Inscripcion inscripcion);
}
