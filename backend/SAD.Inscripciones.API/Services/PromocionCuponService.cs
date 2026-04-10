using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class PromocionCuponService : IPromocionCuponService
{
    private readonly IPromocionCuponRepository _repository;
    private readonly IPromocionRepository _promocionRepository;

    public PromocionCuponService(IPromocionCuponRepository repository, IPromocionRepository promocionRepository)
    {
        _repository = repository;
        _promocionRepository = promocionRepository;
    }

    public async Task<IEnumerable<PromocionCupon>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<PromocionCupon> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("PromocionCupon", id);
    }

    public async Task<IEnumerable<PromocionCupon>> GetByPromocionIdAsync(int promocionId) =>
        await _repository.GetByPromocionIdAsync(promocionId);

    public async Task<IEnumerable<PromocionCupon>> GetByDocumentoAsync(string documento) =>
        await _repository.GetByDocumentoAsync(documento);

    public async Task<PromocionCupon?> GetByCodigoAsync(string codigo) =>
        await _repository.GetByCodigoAsync(codigo);

    public async Task<IEnumerable<PromocionCuponDisponibleDto>> GetDisponiblesByDocumentoAsync(string documento) =>
        await _repository.GetDisponiblesByDocumentoAsync(documento);

    public async Task GenerarCuponesParaInscripcionAsync(Inscripcion inscripcion)
    {
        if (string.IsNullOrEmpty(inscripcion.Documento))
            return;

        var promocionesActivas = await _promocionRepository.GetActiveAsync();

        foreach (var promo in promocionesActivas)
        {
            if (promo.TipoAlumnoId.HasValue && promo.TipoAlumnoId.Value != inscripcion.TipoAlumnoId)
                continue;

            var desde = DateTime.UtcNow.AddMonths(-promo.PeriodoMeses);
            var inscripcionesDisponibles = (await _repository.GetInscripcionesDisponiblesAsync(
                inscripcion.Documento, promo.Id, desde)).ToList();

            if (inscripcionesDisponibles.Count < promo.CantidadCursosRequeridos)
                continue;

            var inscripcionesAConsumir = inscripcionesDisponibles
                .Take(promo.CantidadCursosRequeridos)
                .ToList();

            var cupon = new PromocionCupon
            {
                PromocionId = promo.Id,
                Documento = inscripcion.Documento,
                Codigo = GenerarCodigo(),
                TipoDescuento = promo.TipoDescuento,
                Valor = promo.Valor,
                Acumulable = promo.Acumulable,
                FechaVencimiento = DateTime.UtcNow.AddDays(promo.DiasValidezCupon),
                CreatedBy = "system",
                UpdatedBy = "system"
            };

            var cuponId = await _repository.CreateAsync(cupon);
            await _repository.CreateCuponInscripcionesAsync(cuponId, inscripcionesAConsumir);
        }
    }

    private static string GenerarCodigo()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpper();
    }
}
