using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class TipoEventoService : ITipoEventoService
{
    private readonly ITipoEventoRepository _repository;

    public TipoEventoService(ITipoEventoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TipoEvento>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<TipoEvento> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("TipoEvento", id);
    }

    public async Task<int> CreateAsync(TipoEvento entity)
    {
        var existing = await _repository.GetByNombreAsync(entity.Nombre);
        if (existing != null)
            throw new BusinessException($"Ya existe un tipo de evento con el nombre '{entity.Nombre}'.");
        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(TipoEvento entity)
    {
        await GetByIdAsync(entity.Id);
        var existing = await _repository.GetByNombreAsync(entity.Nombre);
        if (existing != null && existing.Id != entity.Id)
            throw new BusinessException($"Ya existe un tipo de evento con el nombre '{entity.Nombre}'.");
        await _repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }
}
