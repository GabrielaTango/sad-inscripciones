using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class EventoArticuloRegaloService : IEventoArticuloRegaloService
{
    private readonly IEventoArticuloRegaloRepository _repository;
    private readonly IEventoRepository _eventoRepository;
    private readonly ITipoAlumnoRepository _tipoAlumnoRepository;

    public EventoArticuloRegaloService(IEventoArticuloRegaloRepository repository, IEventoRepository eventoRepository, ITipoAlumnoRepository tipoAlumnoRepository)
    {
        _repository = repository;
        _eventoRepository = eventoRepository;
        _tipoAlumnoRepository = tipoAlumnoRepository;
    }

    public async Task<IEnumerable<EventoArticuloRegalo>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<IEnumerable<EventoArticuloRegalo>> GetByEventoIdAsync(int eventoId) => await _repository.GetByEventoIdAsync(eventoId);

    public async Task<EventoArticuloRegalo> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("EventoArticuloRegalo", id);
    }

    public async Task<int> CreateAsync(EventoArticuloRegalo entity)
    {
        if (await _eventoRepository.GetByIdAsync(entity.EventoId) == null)
            throw new BusinessException($"Evento con Id {entity.EventoId} no existe.");
        if (await _tipoAlumnoRepository.GetByIdAsync(entity.TipoAlumnoId) == null)
            throw new BusinessException($"TipoAlumno con Id {entity.TipoAlumnoId} no existe.");
        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(EventoArticuloRegalo entity)
    {
        await GetByIdAsync(entity.Id);
        if (await _eventoRepository.GetByIdAsync(entity.EventoId) == null)
            throw new BusinessException($"Evento con Id {entity.EventoId} no existe.");
        if (await _tipoAlumnoRepository.GetByIdAsync(entity.TipoAlumnoId) == null)
            throw new BusinessException($"TipoAlumno con Id {entity.TipoAlumnoId} no existe.");
        await _repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }
}
