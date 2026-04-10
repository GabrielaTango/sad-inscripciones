using System.Data;
using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Repositories.Interfaces;

public interface IBecaCodigoRepository
{
    Task<IEnumerable<BecaCodigo>> GetAllAsync();
    Task<BecaCodigo?> GetByIdAsync(int id);
    Task<IEnumerable<BecaCodigo>> GetByBecaEventoIdAsync(int becaEventoId);
    Task<BecaCodigo?> GetByCodigoAsync(string codigo);
    Task<int> CreateAsync(BecaCodigo entity);
    Task CreateBatchAsync(IEnumerable<BecaCodigo> entities);
    Task<bool> MarcarUsadoAsync(string codigo, int inscripcionId, IDbConnection connection, IDbTransaction transaction);
    Task<bool> SoftDeleteAsync(int id, string deletedBy);
    Task<IEnumerable<dynamic>> GetCodigosExportAsync(int becaEventoId);
}
