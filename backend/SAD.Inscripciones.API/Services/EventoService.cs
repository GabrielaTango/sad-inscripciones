using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class EventoService : IEventoService
{
    private readonly IEventoRepository _repository;
    private readonly ITipoEventoRepository _tipoEventoRepository;
    private readonly IInscripcionRepository _inscripcionRepository;

    public EventoService(IEventoRepository repository, ITipoEventoRepository tipoEventoRepository, IInscripcionRepository inscripcionRepository)
    {
        _repository = repository;
        _tipoEventoRepository = tipoEventoRepository;
        _inscripcionRepository = inscripcionRepository;
    }

    public async Task<IEnumerable<Evento>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<IEnumerable<Evento>> GetActivosAsync() => await _repository.GetActivosAsync();

    public async Task<Evento> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("Evento", id);
    }

    public async Task<int> CreateAsync(Evento entity)
    {
        var tipoEvento = await _tipoEventoRepository.GetByIdAsync(entity.TipoEventoId);
        if (tipoEvento == null)
            throw new BusinessException($"TipoEvento con Id {entity.TipoEventoId} no existe.");

        if (entity.FechaFin < entity.FechaInicio)
            throw new BusinessException("La fecha de fin no puede ser anterior a la fecha de inicio.");

        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(Evento entity)
    {
        await GetByIdAsync(entity.Id);

        var tipoEvento = await _tipoEventoRepository.GetByIdAsync(entity.TipoEventoId);
        if (tipoEvento == null)
            throw new BusinessException($"TipoEvento con Id {entity.TipoEventoId} no existe.");

        if (entity.FechaFin < entity.FechaInicio)
            throw new BusinessException("La fecha de fin no puede ser anterior a la fecha de inicio.");

        await _repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        var count = await _inscripcionRepository.CountByEventoIdAsync(id);
        if (count > 0)
            throw new BusinessException($"No se puede eliminar el evento porque tiene {count} inscripciones activas.");
        await _repository.SoftDeleteAsync(id, deletedBy);
    }
}
