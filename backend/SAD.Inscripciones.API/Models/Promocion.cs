namespace SAD.Inscripciones.API.Models;

public class Promocion : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? TipoAlumnoId { get; set; }
    public int CantidadCursosRequeridos { get; set; } = 1;
    public int PeriodoMeses { get; set; } = 12;
    public string TipoDescuento { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public bool Acumulable { get; set; }
    public DateTime FechaVigenciaDesde { get; set; }
    public DateTime FechaVigenciaHasta { get; set; }
    public int DiasValidezCupon { get; set; } = 90;
    public bool Activo { get; set; } = true;
}
