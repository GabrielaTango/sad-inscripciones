namespace SAD.Inscripciones.API.Helpers;

public static class PagoFacilCodigo
{
    // Porteo directo de la rutina PHP que se usa hoy para emitir cupones Pago Fácil.
    // Estructura del bloque base (40 dígitos):
    //   2764 | importe(8) | yy(2) | ddd(3) | cuenta(14) | 0 | 000000 | 10
    // Después se concatenan dos dígitos verificadores: el primero sobre los 40
    // dígitos con la clave 1357...(40), el segundo sobre los 41 resultantes con
    // la clave 1357...(41). Total final: 42 dígitos (par, apto para ITF).
    public static string Generar(int idPago, decimal monto, DateTime vencimiento)
    {
        var r = idPago + 900000;
        var cuenta = r.ToString().PadLeft(14, '0');
        var pesos = ((long)Math.Round(monto * 100m, MidpointRounding.AwayFromZero)).ToString();
        var importe = pesos.PadLeft(8, '0');
        var yy = vencimiento.ToString("yy");
        var dias = vencimiento.DayOfYear.ToString().PadLeft(3, '0');

        var codigo = $"2764{importe}{yy}{dias}{cuenta}0{"000000"}10";
        codigo += CalcularDigito(codigo, "1357935793579357935793579357935793579357");
        codigo += CalcularDigito(codigo, "13579357935793579357935793579357935793579");
        return codigo;
    }

    private static string CalcularDigito(string codigo, string pesos)
    {
        var suma = 0;
        for (var i = 0; i < pesos.Length; i++)
            suma += (pesos[i] - '0') * (codigo[i] - '0');
        return ((suma / 2) % 10).ToString();
    }
}
