using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class VentaProductoService : IVentaProductoService
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Regex DniLimpiezaRegex = new(@"[.\s]", RegexOptions.Compiled);

    /// <summary>Horas desde FechaAlta tras las cuales una venta Pendiente sin pago pasa a Impaga.</summary>
    private const int HorasHastaImpaga = 24;

    // Estados de pago de MP que todavia pueden terminar en "approved".
    private static readonly HashSet<string> EstadosPagoEnCurso =
        new() { "pending", "in_process", "authorized", "in_mediation" };

    private readonly IProductoRepository _productoRepository;
    private readonly IVentaProductoRepository _repository;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly IEmailService _emailService;
    private readonly ILogger<VentaProductoService> _logger;

    public VentaProductoService(
        IProductoRepository productoRepository,
        IVentaProductoRepository repository,
        IMercadoPagoService mercadoPagoService,
        IEmailService emailService,
        ILogger<VentaProductoService> logger)
    {
        _productoRepository = productoRepository;
        _repository = repository;
        _mercadoPagoService = mercadoPagoService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<VentaProductoCreateResultDto> CrearAsync(VentaProductoCreateDto dto)
    {
        var producto = await _productoRepository.GetByIdAsync(dto.ProductoId);
        if (producto == null || !producto.Activo)
            throw new NotFoundException("Producto", dto.ProductoId);

        var dni = DniLimpiezaRegex.Replace(dto.Dni?.Trim() ?? string.Empty, string.Empty);
        if (dni.Length < 7 || dni.Length > 8 || !dni.All(char.IsDigit))
            throw new BusinessException("El DNI debe tener 7 u 8 dígitos.");

        var nombre = dto.Nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(nombre))
            throw new BusinessException("El nombre es obligatorio.");

        var apellido = dto.Apellido?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(apellido))
            throw new BusinessException("El apellido es obligatorio.");

        var email = dto.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(email) || !EmailRegex.IsMatch(email))
            throw new BusinessException("El email no es válido.");

        var camposExtra = DeserializeCamposExtra(producto.CamposExtra);
        var datosExtra = ValidarDatosExtra(camposExtra, dto.DatosExtra ?? new Dictionary<string, string>());

        var venta = new VentaProducto
        {
            ProductoId = producto.Id,
            PublicRef = Guid.NewGuid().ToString("N"),
            Dni = dni,
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            DatosExtra = JsonSerializer.Serialize(datosExtra),
            // El importe siempre viene del producto, nunca del comprador.
            Importe = producto.Precio,
        };

        venta.Id = await _repository.CreatePendienteAsync(venta);

        var preferencia = await _mercadoPagoService.CrearPreferenciaVentaAsync(venta, producto.Nombre);

        return new VentaProductoCreateResultDto
        {
            VentaId = venta.Id,
            PublicRef = venta.PublicRef,
            InitPoint = preferencia.InitPoint,
        };
    }

    public async Task ProcesarPagoAsync(MercadoPagoPaymentInfo paymentInfo)
    {
        if (!VentaExternalReference.TryParse(paymentInfo.ExternalReference, out var ventaId, out var publicRef))
            return; // No es una referencia de venta de producto (p.ej. es de una inscripcion): se ignora.

        var venta = await _repository.GetByIdAsync(ventaId);
        if (venta is null)
        {
            _logger.LogWarning("Venta {VentaId} no encontrada para pago MP {PaymentId}", ventaId, paymentInfo.Id);
            return;
        }

        if (venta.PublicRef != publicRef)
        {
            _logger.LogWarning(
                "PublicRef no coincide para venta {VentaId}: esperado {Esperado}, recibido {Recibido} (pago MP {PaymentId})",
                ventaId, venta.PublicRef, publicRef, paymentInfo.Id);
            return;
        }

        // Decision de producto (ver odd/tasks/productos-venta.md): un pago no aprobado
        // NO cambia el estado de la venta, solo se loguea. MP permite reintentar sobre la
        // misma preferencia, y un reintento posterior aprobado debe poder confirmar la
        // venta igual, así que nunca queda "cerrada" por un rechazo intermedio.
        if (paymentInfo.Status != "approved")
        {
            _logger.LogInformation(
                "Pago MP {PaymentId} para venta {VentaId} con status \"{Status}\": no se confirma, la venta queda Pendiente.",
                paymentInfo.Id, ventaId, paymentInfo.Status);
            return;
        }

        var resultado = await _repository.ConfirmarPagoAsync(ventaId, paymentInfo.Id, paymentInfo.TransactionAmount);

        switch (resultado)
        {
            case ConfirmarPagoResult.Confirmed:
                _logger.LogInformation("Venta {VentaId} confirmada por pago MP {PaymentId}", ventaId, paymentInfo.Id);
                await EnviarMailConfirmacionAsync(venta);
                break;
            case ConfirmarPagoResult.AmountMismatch:
                _logger.LogWarning(
                    "Monto no coincide para venta {VentaId}: importe={Importe}, acreditado={Acreditado} (pago MP {PaymentId})",
                    ventaId, venta.Importe, paymentInfo.TransactionAmount, paymentInfo.Id);
                break;
            case ConfirmarPagoResult.AlreadyPaid:
                _logger.LogInformation("Venta {VentaId} ya estaba confirmada (llamada idempotente)", ventaId);
                break;
            case ConfirmarPagoResult.NotFound:
                _logger.LogWarning("Venta {VentaId} no encontrada al confirmar pago MP {PaymentId}", ventaId, paymentInfo.Id);
                break;
        }
    }

    /// <summary>
    /// Dispara el mail de confirmación solo cuando ConfirmarPagoAsync transicionó
    /// Pendiente -&gt; Pagada (nunca en un replay idempotente). Si el envío tuvo éxito,
    /// marca MailEnviado para que quede registro; EnviarConfirmacionVentaAsync ya
    /// loguea y no tira si falla, así que un error de mail nunca revierte la confirmación.
    /// </summary>
    private async Task EnviarMailConfirmacionAsync(VentaProducto venta)
    {
        var producto = await _productoRepository.GetByIdAsync(venta.ProductoId);
        if (producto is null)
        {
            _logger.LogWarning("Producto {ProductoId} no encontrado; no se envía mail para venta {VentaId}", venta.ProductoId, venta.Id);
            return;
        }

        var enviado = await _emailService.EnviarConfirmacionVentaAsync(venta, producto);
        if (enviado)
            await _repository.MarcarMailEnviadoAsync(venta.Id);
    }

    public async Task<VentaProductoEstadoDto> VerificarAsync(string publicRef)
    {
        var venta = await _repository.GetByPublicRefAsync(publicRef);
        if (venta is null)
            throw new NotFoundException($"VentaProducto con PublicRef {publicRef} no encontrada.");

        // Una venta Impaga tambien se reconsulta: si el comprador vuelve y el pago
        // quedo aprobado, ProcesarPagoAsync la confirma.
        if (venta.Estado != "Pagada")
        {
            var externalReference = VentaExternalReference.Build(venta.Id, venta.PublicRef);
            var pagos = await _mercadoPagoService.BuscarTodosPagosPorReferenciaAsync(externalReference);
            foreach (var pago in pagos.Where(p => p.Status == "approved"))
            {
                await ProcesarPagoAsync(pago);
            }

            // Releer: ProcesarPagoAsync puede haber confirmado la venta.
            venta = await _repository.GetByIdAsync(venta.Id) ?? venta;
        }

        var producto = await _productoRepository.GetByIdAsync(venta.ProductoId);

        return new VentaProductoEstadoDto
        {
            Estado = venta.Estado,
            ProductoId = venta.ProductoId,
            ProductoNombre = producto?.Nombre ?? string.Empty,
            Nombre = venta.Nombre,
            Importe = venta.Importe,
        };
    }

    public async Task<VentaProductoConsultaResultadoDto> ConsultarPendientesAsync()
    {
        var resultado = new VentaProductoConsultaResultadoDto();
        var pendientes = (await _repository.ListPendientesAsync()).ToList();

        foreach (var venta in pendientes)
        {
            resultado.Consultadas++;
            try
            {
                var externalReference = VentaExternalReference.Build(venta.Id, venta.PublicRef);
                var pagos = await _mercadoPagoService.BuscarTodosPagosPorReferenciaAsync(externalReference);

                var aprobados = pagos.Where(p => p.Status == "approved").ToList();
                foreach (var pago in aprobados)
                {
                    await ProcesarPagoAsync(pago);
                }

                var actual = await _repository.GetByIdAsync(venta.Id);
                if (actual is null)
                {
                    resultado.Errores++;
                    continue;
                }

                if (actual.Estado == "Pagada")
                {
                    resultado.Pagadas++;
                    continue;
                }

                if (actual.Estado != "Pendiente")
                {
                    // Otro proceso la movio mientras tanto: no se cuenta ni se toca.
                    resultado.SiguenPendientes++;
                    continue;
                }

                if (aprobados.Count > 0)
                {
                    // Hay un pago aprobado que no confirmo la venta (monto distinto u otro
                    // motivo, ya logueado por ProcesarPagoAsync): se cobro plata, asi que
                    // nunca se marca Impaga; queda Pendiente para revision manual.
                    resultado.Errores++;
                    continue;
                }

                if (pagos.Any(p => EstadosPagoEnCurso.Contains(p.Status)))
                {
                    resultado.SiguenPendientes++;
                    continue;
                }

                // La antiguedad se evalua en SQL: false significa que aun no pasaron las horas minimas.
                if (await _repository.MarcarImpagaAsync(venta.Id, HorasHastaImpaga))
                    resultado.Impagas++;
                else
                    resultado.SiguenPendientes++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar la venta {VentaId} contra MercadoPago", venta.Id);
                resultado.Errores++;
            }
        }

        return resultado;
    }

    /// <summary>
    /// Valida las respuestas del comprador contra el esquema CamposExtra del
    /// producto: los campos requeridos deben estar presentes y no vacíos, los
    /// numéricos deben parsear y los select deben caer dentro de sus opciones.
    /// Cualquier clave de <paramref name="datos"/> que no esté en el esquema se
    /// descarta silenciosamente.
    /// </summary>
    private static Dictionary<string, string> ValidarDatosExtra(List<CampoExtraProducto> camposExtra, Dictionary<string, string> datos)
    {
        var resultado = new Dictionary<string, string>();

        foreach (var campo in camposExtra)
        {
            var valor = datos.TryGetValue(campo.Key, out var valorRaw) ? valorRaw?.Trim() ?? string.Empty : string.Empty;

            if (campo.Required && string.IsNullOrEmpty(valor))
                throw new BusinessException($"El campo \"{campo.Label}\" es obligatorio.");

            if (string.IsNullOrEmpty(valor))
                continue;

            switch (campo.Type)
            {
                case "number":
                    if (!decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        throw new BusinessException($"El campo \"{campo.Label}\" debe ser numérico.");
                    break;
                case "select":
                    if (!campo.Options.Contains(valor))
                        throw new BusinessException($"El campo \"{campo.Label}\" tiene un valor inválido.");
                    break;
            }

            resultado[campo.Key] = valor;
        }

        return resultado;
    }

    private static List<CampoExtraProducto> DeserializeCamposExtra(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<CampoExtraProducto>();

        return JsonSerializer.Deserialize<List<CampoExtraProducto>>(json) ?? new List<CampoExtraProducto>();
    }

    public async Task<IEnumerable<VentaProductoAdminDto>> ListAdminAsync(int? productoId, string? estado, DateTime? desde, DateTime? hasta, string? texto)
    {
        var filas = await _repository.ListAsync(productoId, estado, desde, hasta, texto);
        return filas.Select(MapAdminDto);
    }

    private static VentaProductoAdminDto MapAdminDto(VentaProductoAdminRow fila) => new()
    {
        Id = fila.Id,
        ProductoId = fila.ProductoId,
        ProductoNombre = fila.ProductoNombre,
        Dni = fila.Dni,
        Nombre = fila.Nombre,
        Apellido = fila.Apellido,
        Email = fila.Email,
        DatosExtra = DeserializeDatosExtra(fila.DatosExtra),
        Importe = fila.Importe,
        Estado = fila.Estado,
        MpPaymentId = fila.MpPaymentId,
        FechaAlta = fila.FechaAlta,
        FechaPago = fila.FechaPago,
        MailEnviado = fila.MailEnviado,
        UpdatedAt = fila.UpdatedAt,
    };

    private static Dictionary<string, string> DeserializeDatosExtra(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    public async Task<byte[]> ExportToExcelAsync(int? productoId, string? estado, DateTime? desde, DateTime? hasta, string? texto)
    {
        var ventas = (await ListAdminAsync(productoId, estado, desde, hasta, texto)).ToList();

        // Columnas de campos extra: las del producto filtrado (en su orden declarado), o
        // la union de claves presentes en los resultados cuando no hay filtro de producto.
        var columnasExtra = new List<(string Key, string Label)>();
        if (productoId.HasValue)
        {
            var producto = await _productoRepository.GetByIdAsync(productoId.Value);
            if (producto != null)
                columnasExtra.AddRange(DeserializeCamposExtra(producto.CamposExtra).Select(c => (c.Key, c.Label)));
        }
        else
        {
            var vistos = new HashSet<string>();
            foreach (var venta in ventas)
            {
                foreach (var key in venta.DatosExtra.Keys)
                {
                    if (vistos.Add(key))
                        columnasExtra.Add((key, key));
                }
            }
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Ventas");

        var headers = new List<string> { "Estado", "Fecha alta", "Fecha pago", "Producto", "DNI", "Apellido", "Nombre", "Email" };
        headers.AddRange(columnasExtra.Select(c => c.Label));
        headers.Add("Importe");
        headers.Add("Nro pago MP");
        headers.Add("Mail enviado");

        for (int c = 0; c < headers.Count; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var headerRange = ws.Range(1, 1, 1, headers.Count);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0x5D, 0x8A, 0xC8);

        for (int i = 0; i < ventas.Count; i++)
        {
            var venta = ventas[i];
            int row = i + 2;
            int col = 1;

            ws.Cell(row, col++).Value = venta.Estado;
            ws.Cell(row, col++).Value = venta.FechaAlta.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, col++).Value = venta.FechaPago?.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Cell(row, col++).Value = venta.ProductoNombre;
            ws.Cell(row, col++).Value = venta.Dni;
            ws.Cell(row, col++).Value = venta.Apellido;
            ws.Cell(row, col++).Value = venta.Nombre;
            ws.Cell(row, col++).Value = venta.Email;

            foreach (var columna in columnasExtra)
                ws.Cell(row, col++).Value = venta.DatosExtra.TryGetValue(columna.Key, out var valor) ? valor : "";

            ws.Cell(row, col++).Value = venta.Importe;
            ws.Cell(row, col++).Value = venta.MpPaymentId?.ToString() ?? "";
            ws.Cell(row, col++).Value = venta.MailEnviado ? "Si" : "No";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
