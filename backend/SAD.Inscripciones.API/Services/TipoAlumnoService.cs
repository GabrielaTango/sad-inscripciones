using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class TipoAlumnoService : ITipoAlumnoService
{
    private readonly ITipoAlumnoRepository _repository;

    public TipoAlumnoService(ITipoAlumnoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TipoAlumno>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<TipoAlumno> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("TipoAlumno", id);
    }

    public async Task<int> CreateAsync(TipoAlumno entity)
    {
        var existing = await _repository.GetByNombreAsync(entity.Nombre);
        if (existing != null)
            throw new BusinessException($"Ya existe un tipo de alumno con el nombre '{entity.Nombre}'.");
        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateAsync(TipoAlumno entity)
    {
        await GetByIdAsync(entity.Id);
        var existing = await _repository.GetByNombreAsync(entity.Nombre);
        if (existing != null && existing.Id != entity.Id)
            throw new BusinessException($"Ya existe un tipo de alumno con el nombre '{entity.Nombre}'.");
        await _repository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }
}
