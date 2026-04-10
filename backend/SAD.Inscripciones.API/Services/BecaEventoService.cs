using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class BecaEventoService : IBecaEventoService
{
    private readonly IBecaEventoRepository _repository;
    private readonly IBecaCodigoRepository _becaCodigoRepository;
    private readonly IEventoRepository _eventoRepository;

    public BecaEventoService(IBecaEventoRepository repository, IBecaCodigoRepository becaCodigoRepository, IEventoRepository eventoRepository)
    {
        _repository = repository;
        _becaCodigoRepository = becaCodigoRepository;
        _eventoRepository = eventoRepository;
    }

    public async Task<IEnumerable<BecaEvento>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<BecaEvento> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("BecaEvento", id);
    }

    public async Task<IEnumerable<BecaEvento>> GetByEventoIdAsync(int eventoId) => await _repository.GetByEventoIdAsync(eventoId);

    public async Task<int> CreateAsync(BecaEvento entity, string createdBy)
    {
        if (await _eventoRepository.GetByIdAsync(entity.EventoId) == null)
            throw new BusinessException($"Evento con Id {entity.EventoId} no existe.");

        if (entity.TipoDescuento != "Porcentaje" && entity.TipoDescuento != "MontoFijo")
            throw new BusinessException("TipoDescuento debe ser 'Porcentaje' o 'MontoFijo'.");

        if (entity.TipoDescuento == "Porcentaje" && (entity.Valor < 0 || entity.Valor > 100))
            throw new BusinessException("El porcentaje debe estar entre 0 y 100.");

        entity.CreatedBy = createdBy;
        entity.UpdatedBy = createdBy;
        var id = await _repository.CreateAsync(entity);

        var codigos = new List<BecaCodigo>();
        for (int i = 0; i < entity.CantidadTotalCodigos; i++)
        {
            codigos.Add(new BecaCodigo
            {
                BecaEventoId = id,
                Codigo = GenerarCodigo(),
                CreatedBy = createdBy,
                UpdatedBy = createdBy
            });
        }
        await _becaCodigoRepository.CreateBatchAsync(codigos);

        return id;
    }

    public async Task UpdateAsync(BecaEvento entity)
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

    private static string GenerarCodigo()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpper();
    }
}
