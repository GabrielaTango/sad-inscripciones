using SAD.Inscripciones.API.Models;

namespace SAD.Inscripciones.API.Services.Interfaces;

public interface IBecaCodigoService
{
    Task<IEnumerable<BecaCodigo>> GetAllAsync();
    Task<BecaCodigo> GetByIdAsync(int id);
    Task<IEnumerable<BecaCodigo>> GetByBecaEventoIdAsync(int becaEventoId);
    Task<BecaCodigo?> GetByCodigoAsync(string codigo);
    Task<byte[]> ExportToExcelAsync(int becaEventoId);
}
