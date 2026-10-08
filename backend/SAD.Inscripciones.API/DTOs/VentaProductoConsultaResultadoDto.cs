namespace SAD.Inscripciones.API.DTOs;

/// <summary>Resumen de la consulta masiva de ventas Pendiente contra MercadoPago.</summary>
public class VentaProductoConsultaResultadoDto
{
    public int Consultadas { get; set; }
    public int Pagadas { get; set; }
    public int Impagas { get; set; }
    public int SiguenPendientes { get; set; }
    public int Errores { get; set; }
}
