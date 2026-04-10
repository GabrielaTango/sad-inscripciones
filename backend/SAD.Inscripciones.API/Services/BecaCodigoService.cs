using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;
using SpreadsheetLight;

namespace SAD.Inscripciones.API.Services;

public class BecaCodigoService : IBecaCodigoService
{
    private readonly IBecaCodigoRepository _repository;

    public BecaCodigoService(IBecaCodigoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BecaCodigo>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<BecaCodigo> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("BecaCodigo", id);
    }

    public async Task<IEnumerable<BecaCodigo>> GetByBecaEventoIdAsync(int becaEventoId) => await _repository.GetByBecaEventoIdAsync(becaEventoId);

    public async Task<BecaCodigo?> GetByCodigoAsync(string codigo) => await _repository.GetByCodigoAsync(codigo);

    public async Task<byte[]> ExportToExcelAsync(int becaEventoId)
    {
        var datos = (await _repository.GetCodigosExportAsync(becaEventoId)).ToList();

        using var sl = new SLDocument();

        // Headers
        sl.SetCellValue("A1", "Campaña");
        sl.SetCellValue("B1", "Evento");
        sl.SetCellValue("C1", "Código");
        sl.SetCellValue("D1", "Fecha Vencimiento");

        // Header style
        var headerStyle = sl.CreateStyle();
        headerStyle.Font.Bold = true;
        sl.SetCellStyle("A1", "D1", headerStyle);

        for (int i = 0; i < datos.Count; i++)
        {
            int row = i + 2;
            var d = (IDictionary<string, object>)datos[i];

            sl.SetCellValue($"A{row}", d["NombreCampana"]?.ToString() ?? "");
            sl.SetCellValue($"B{row}", d["NombreEvento"]?.ToString() ?? "");
            sl.SetCellValue($"C{row}", d["Codigo"]?.ToString() ?? "");
            sl.SetCellValue($"D{row}", d["FechaVencimiento"] != null ? Convert.ToDateTime(d["FechaVencimiento"]).ToString("dd/MM/yyyy") : "Sin vencimiento");
        }

        // Auto-fit columns
        for (int col = 1; col <= 4; col++)
            sl.AutoFitColumn(col);

        using var ms = new MemoryStream();
        sl.SaveAs(ms);
        return ms.ToArray();
    }
}
