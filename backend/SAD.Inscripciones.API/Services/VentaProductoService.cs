using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    private readonly IProductoRepository _productoRepository;
    private readonly IVentaProductoRepository _repository;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly ILogger<VentaProductoService> _logger;

    public VentaProductoService(
        IProductoRepository productoRepository,
        IVentaProductoRepository repository,
        IMercadoPagoService mercadoPagoService,
        ILogger<VentaProductoService> logger)
    {
        _productoRepository = productoRepository;
        _repository = repository;
        _mercadoPagoService = mercadoPagoService;
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

    public async Task<VentaProductoEstadoDto> VerificarAsync(string publicRef)
    {
        var venta = await _repository.GetByPublicRefAsync(publicRef);
        if (venta is null)
            throw new NotFoundException($"VentaProducto con PublicRef {publicRef} no encontrada.");

        if (venta.Estado == "Pendiente")
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
}
