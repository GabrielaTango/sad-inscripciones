using MySqlConnector;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class PayPalService : IPayPalService
{
    private const string EstadoConfirmado = "Confirmado";
    private const string MonedaUsd = "USD";

    private readonly IInscripcionRepository _inscripcionRepository;
    private readonly IInscripcionService _inscripcionService;
    private readonly IPagoRepository _pagoRepository;
    private readonly ILogger<PayPalService> _logger;

    public PayPalService(
        IInscripcionRepository inscripcionRepository,
        IInscripcionService inscripcionService,
        IPagoRepository pagoRepository,
        ILogger<PayPalService> logger)
    {
        _inscripcionRepository = inscripcionRepository;
        _inscripcionService = inscripcionService;
        _pagoRepository = pagoRepository;
        _logger = logger;
    }

    public async Task<ConfirmarPagoPayPalResult> ConfirmarPagoAsync(ConfirmarPagoPayPalDto dto, string actor)
    {
        if (string.IsNullOrWhiteSpace(dto.OrderId))
            throw new ArgumentException("OrderId requerido", nameof(dto));

        var inscripcion = await _inscripcionRepository.GetByIdAsync(dto.InscripcionId)
            ?? throw new ArgumentException($"Inscripcion {dto.InscripcionId} no encontrada", nameof(dto));

        var orderId = dto.OrderId.Trim();
        var existentes = (await _pagoRepository.GetByInscripcionIdAsync(dto.InscripcionId))
            .Where(p => p.DeletedAt == null)
            .ToList();

        var yaRegistrado = existentes.Any(p => p.ReferenciaExterna == orderId);
        if (!yaRegistrado)
        {
            try
            {
                await _pagoRepository.CreateAsync(new Pago
                {
                    InscripcionId = dto.InscripcionId,
                    MedioPago = "PayPal",
                    EstadoPago = EstadoConfirmado,
                    Monto = dto.Monto,
                    Moneda = MonedaUsd,
                    ReferenciaExterna = orderId,
                    FechaPago = DateTime.UtcNow,
                    Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
                        ? $"PayPal Order {orderId}"
                        : dto.Observaciones,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Otra ejecución concurrente ya creó el pago para esta order: idempotente.
                _logger.LogDebug("Pago PayPal {OrderId} ya existía (inscripcion={Id})", orderId, dto.InscripcionId);
            }
        }

        if (inscripcion.Estado != "Confirmada")
        {
            // Pasamos por el service para disparar side-effects (cupones, mail).
            await _inscripcionService.UpdateEstadoAsync(dto.InscripcionId, "Confirmada", actor);
            _logger.LogInformation(
                "Inscripcion {Id}: {Old} → Confirmada vía PayPal (order={OrderId}, monto={Monto} USD)",
                dto.InscripcionId, inscripcion.Estado, orderId, dto.Monto);
        }

        return new ConfirmarPagoPayPalResult
        {
            InscripcionId = dto.InscripcionId,
            EstadoInscripcion = "Confirmada",
            EstadoPago = EstadoConfirmado,
            Monto = dto.Monto,
            Moneda = MonedaUsd,
        };
    }
}
