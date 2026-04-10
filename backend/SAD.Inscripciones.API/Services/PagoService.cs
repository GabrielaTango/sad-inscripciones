using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class PagoService : IPagoService
{
    private readonly IPagoRepository _repository;
    private readonly IInscripcionRepository _inscripcionRepository;

    public PagoService(IPagoRepository repository, IInscripcionRepository inscripcionRepository)
    {
        _repository = repository;
        _inscripcionRepository = inscripcionRepository;
    }

    public async Task<IEnumerable<Pago>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<Pago> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("Pago", id);
    }

    public async Task<IEnumerable<Pago>> GetByInscripcionIdAsync(int inscripcionId) => await _repository.GetByInscripcionIdAsync(inscripcionId);

    public async Task<int> CreateAsync(Pago entity)
    {
        var inscripcion = await _inscripcionRepository.GetByIdAsync(entity.InscripcionId);
        if (inscripcion == null)
            throw new BusinessException($"Inscripcion con Id {entity.InscripcionId} no existe.");
        return await _repository.CreateAsync(entity);
    }

    public async Task UpdateEstadoAsync(int id, string estadoPago, string updatedBy)
    {
        var pago = await GetByIdAsync(id);
        pago.EstadoPago = estadoPago;
        pago.UpdatedBy = updatedBy;

        if (estadoPago == "Confirmado")
            pago.FechaPago = DateTime.UtcNow;

        await _repository.UpdateAsync(pago);

        if (estadoPago == "Confirmado")
        {
            await _inscripcionRepository.UpdateEstadoAsync(pago.InscripcionId, "Confirmada", updatedBy);
        }
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }
}
