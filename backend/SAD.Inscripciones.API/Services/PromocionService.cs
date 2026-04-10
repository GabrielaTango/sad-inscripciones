using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class PromocionService : IPromocionService
{
    private readonly IPromocionRepository _repository;
    private readonly ITipoAlumnoRepository _tipoAlumnoRepository;

    public PromocionService(IPromocionRepository repository, ITipoAlumnoRepository tipoAlumnoRepository)
    {
        _repository = repository;
        _tipoAlumnoRepository = tipoAlumnoRepository;
    }

    public async Task<IEnumerable<Promocion>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<Promocion> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("Promocion", id);
    }

    public async Task<int> CreateAsync(Promocion entity, string createdBy)
    {
        ValidarPromocion(entity);

        if (entity.TipoAlumnoId.HasValue && await _tipoAlumnoRepository.GetByIdAsync(entity.TipoAlumnoId.Value) == null)
            throw new BusinessException($"TipoAlumno con Id {entity.TipoAlumnoId} no existe.");

        entity.CreatedBy = createdBy;
        entity.UpdatedBy = createdBy;
        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(Promocion entity)
    {
        await GetByIdAsync(entity.Id);
        ValidarPromocion(entity);

        if (entity.TipoAlumnoId.HasValue && await _tipoAlumnoRepository.GetByIdAsync(entity.TipoAlumnoId.Value) == null)
            throw new BusinessException($"TipoAlumno con Id {entity.TipoAlumnoId} no existe.");

        await _repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }

    private static void ValidarPromocion(Promocion entity)
    {
        if (entity.TipoDescuento != "Porcentaje" && entity.TipoDescuento != "MontoFijo")
            throw new BusinessException("TipoDescuento debe ser 'Porcentaje' o 'MontoFijo'.");

        if (entity.TipoDescuento == "Porcentaje" && (entity.Valor < 0 || entity.Valor > 100))
            throw new BusinessException("Para descuento porcentual, el valor debe estar entre 0 y 100.");

        if (entity.FechaVigenciaHasta <= entity.FechaVigenciaDesde)
            throw new BusinessException("FechaVigenciaHasta debe ser posterior a FechaVigenciaDesde.");
    }
}
