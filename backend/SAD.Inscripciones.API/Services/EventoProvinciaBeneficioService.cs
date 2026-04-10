using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class EventoProvinciaBeneficioService : IEventoProvinciaBeneficioService
{
    private readonly IEventoProvinciaBeneficioRepository _repository;
    private readonly IEventoRepository _eventoRepository;

    public EventoProvinciaBeneficioService(IEventoProvinciaBeneficioRepository repository, IEventoRepository eventoRepository)
    {
        _repository = repository;
        _eventoRepository = eventoRepository;
    }

    public async Task<IEnumerable<EventoProvinciaBeneficio>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<IEnumerable<EventoProvinciaBeneficio>> GetByEventoIdAsync(int eventoId) => await _repository.GetByEventoIdAsync(eventoId);

    public async Task<EventoProvinciaBeneficio> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("EventoProvinciaBeneficio", id);
    }

    public async Task<int> CreateAsync(EventoProvinciaBeneficio entity)
    {
        if (await _eventoRepository.GetByIdAsync(entity.EventoId) == null)
            throw new BusinessException($"Evento con Id {entity.EventoId} no existe.");
        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(EventoProvinciaBeneficio entity)
    {
        await GetByIdAsync(entity.Id);
        if (await _eventoRepository.GetByIdAsync(entity.EventoId) == null)
            throw new BusinessException($"Evento con Id {entity.EventoId} no existe.");
        await _repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }
}
