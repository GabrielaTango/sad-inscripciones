namespace SAD.Inscripciones.API.Models;

/// <summary>
/// One entry of a Producto's dynamic extra-field schema (Productos.CamposExtra),
/// serialized as JSON. Rendered on the public purchase form and answered into
/// VentaProducto.DatosExtra.
/// </summary>
public class CampoExtraProducto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    /// <summary>"text", "number" or "select".</summary>
    public string Type { get; set; } = "text";

    /// <summary>Only used when Type == "select".</summary>
    public List<string> Options { get; set; } = new();

    public bool Required { get; set; }
}
