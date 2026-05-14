using ClosedXML.Excel;
using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

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

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Becas");

        ws.Cell(1, 1).Value = "Campaña";
        ws.Cell(1, 2).Value = "Evento";
        ws.Cell(1, 3).Value = "Código";
        ws.Cell(1, 4).Value = "Fecha Vencimiento";

        var headerRange = ws.Range(1, 1, 1, 4);
        headerRange.Style.Font.Bold = true;

        for (int i = 0; i < datos.Count; i++)
        {
            int row = i + 2;
            var d = (IDictionary<string, object>)datos[i];

            ws.Cell(row, 1).Value = d["NombreCampana"]?.ToString() ?? "";
            ws.Cell(row, 2).Value = d["NombreEvento"]?.ToString() ?? "";
            ws.Cell(row, 3).Value = d["Codigo"]?.ToString() ?? "";
            ws.Cell(row, 4).Value = d["FechaVencimiento"] != null ? Convert.ToDateTime(d["FechaVencimiento"]).ToString("dd/MM/yyyy") : "Sin vencimiento";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
