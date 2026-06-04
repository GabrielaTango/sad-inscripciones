using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class EventoPrecioService : IEventoPrecioService
{
    private readonly IEventoPrecioRepository _repository;
    private readonly IEventoRepository _eventoRepository;
    private readonly ITipoAlumnoRepository _tipoAlumnoRepository;
    private readonly IInscripcionRepository _inscripcionRepository;

    public EventoPrecioService(IEventoPrecioRepository repository, IEventoRepository eventoRepository, ITipoAlumnoRepository tipoAlumnoRepository, IInscripcionRepository inscripcionRepository)
    {
        _repository = repository;
        _eventoRepository = eventoRepository;
        _tipoAlumnoRepository = tipoAlumnoRepository;
        _inscripcionRepository = inscripcionRepository;
    }

    public async Task<IEnumerable<EventoPrecio>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<IEnumerable<EventoPrecio>> GetByEventoIdAsync(int eventoId) => await _repository.GetByEventoIdAsync(eventoId);

    public async Task<EventoPrecio> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("EventoPrecio", id);
    }

    public async Task<int> CreateAsync(EventoPrecio entity)
    {
        if (await _eventoRepository.GetByIdAsync(entity.EventoId) == null)
            throw new BusinessException($"Evento con Id {entity.EventoId} no existe.");
        if (await _tipoAlumnoRepository.GetByIdAsync(entity.TipoAlumnoId) == null)
            throw new BusinessException($"TipoAlumno con Id {entity.TipoAlumnoId} no existe.");

        var existing = await _repository.GetByEventoAndTipoAlumnoAsync(entity.EventoId, entity.TipoAlumnoId);
        if (existing != null)
            throw new BusinessException("Ya existe un precio para este evento y tipo de alumno.");

        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(EventoPrecio entity)
    {
        await GetByIdAsync(entity.Id);
        if (await _eventoRepository.GetByIdAsync(entity.EventoId) == null)
            throw new BusinessException($"Evento con Id {entity.EventoId} no existe.");
        if (await _tipoAlumnoRepository.GetByIdAsync(entity.TipoAlumnoId) == null)
            throw new BusinessException($"TipoAlumno con Id {entity.TipoAlumnoId} no existe.");
        await _repository.UpdateAsync(entity);

        // Propaga el nuevo precio a las inscripciones aún no cobradas (Reservada/Pendiente) de este
        // evento + tipo de alumno, manteniendo el mismo % de descuento. Las Confirmadas no se tocan
        // porque suelen tener pago hecho y/o factura ya sincronizada a Tango.
        await _inscripcionRepository.RecalcularPreciosByEventoTipoAlumnoAsync(
            entity.EventoId, entity.TipoAlumnoId, entity.PrecioBase, entity.PrecioCuotas, entity.CantidadCuotas, entity.UpdatedBy);
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }
}
