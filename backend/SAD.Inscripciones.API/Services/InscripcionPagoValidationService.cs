using MySqlConnector;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class InscripcionPagoValidationService : IInscripcionPagoValidationService
{
    private const string EstadoAprobado = "Confirmado";
    private const string EstadoRechazado = "Rechazado";
    private const string EstadoPendiente = "Pendiente";
    private const string ActorValidacion = "validacion-mp";
    private const decimal Tolerancia = 0.01m;

    private readonly IInscripcionRepository _inscripcionRepository;
    private readonly IInscripcionService _inscripcionService;
    private readonly IPagoRepository _pagoRepository;
    private readonly IMercadoPagoService _mpService;
    private readonly ILogger<InscripcionPagoValidationService> _logger;

    public InscripcionPagoValidationService(
        IInscripcionRepository inscripcionRepository,
        IInscripcionService inscripcionService,
        IPagoRepository pagoRepository,
        IMercadoPagoService mpService,
        ILogger<InscripcionPagoValidationService> logger)
    {
        _inscripcionRepository = inscripcionRepository;
        _inscripcionService = inscripcionService;
        _pagoRepository = pagoRepository;
        _mpService = mpService;
        _logger = logger;
    }

    public async Task<ValidacionInscripcionResult> ValidarInscripcionAsync(int inscripcionId)
    {
        var inscripcion = await _inscripcionRepository.GetByIdAsync(inscripcionId)
            ?? throw new ArgumentException($"Inscripcion {inscripcionId} no encontrada", nameof(inscripcionId));

        var estadoAnterior = inscripcion.Estado;
        var mpPagos = await _mpService.BuscarTodosPagosPorReferenciaAsync(inscripcionId.ToString());

        var existentes = (await _pagoRepository.GetByInscripcionIdAsync(inscripcionId))
            .Where(p => p.DeletedAt == null)
            .ToList();

        var pagosNuevos = 0;
        foreach (var mp in mpPagos)
        {
            var refExt = mp.Id.ToString();
            var estado = MapEstadoMP(mp.Status);
            var existente = existentes.FirstOrDefault(p => p.ReferenciaExterna == refExt);

            if (existente != null)
            {
                if (existente.EstadoPago != estado)
                {
                    existente.EstadoPago = estado;
                    existente.UpdatedBy = ActorValidacion;
                    if (estado == EstadoAprobado && existente.FechaPago == null)
                        existente.FechaPago = DateTime.UtcNow;
                    await _pagoRepository.UpdateAsync(existente);
                }
            }
            else
            {
                try
                {
                    await _pagoRepository.CreateAsync(new Pago
                    {
                        InscripcionId = inscripcionId,
                        MedioPago = $"MercadoPago ({mp.PaymentMethodId})",
                        EstadoPago = estado,
                        Monto = mp.TransactionAmount,
                        ReferenciaExterna = refExt,
                        FechaPago = estado == EstadoAprobado ? DateTime.UtcNow : null,
                        Observaciones = $"MP Payment {mp.Id} - {mp.Status}/{mp.StatusDetail}",
                        CreatedBy = ActorValidacion,
                        UpdatedBy = ActorValidacion,
                    });
                    pagosNuevos++;
                }
                catch (MySqlException ex) when (ex.Number == 1062)
                {
                    _logger.LogDebug(
                        "Pago MP {PaymentId} ya fue creado por otra ejecución concurrente (ref={Ref})",
                        mp.Id, refExt);
                }
            }
        }

        var todosPagos = await _pagoRepository.GetByInscripcionIdAsync(inscripcionId);
        var sumApproved = todosPagos
            .Where(p => p.EstadoPago == EstadoAprobado && p.DeletedAt == null)
            .Sum(p => p.Monto);

        var precioFinal = inscripcion.PrecioFinal;
        var montoReserva = inscripcion.MontoReserva ?? 0;

        string estadoNuevo;
        if (precioFinal > 0 && sumApproved + Tolerancia >= precioFinal)
            estadoNuevo = "Confirmada";
        else if (montoReserva > 0 && sumApproved + Tolerancia >= montoReserva)
            estadoNuevo = "Reservada";
        else
            estadoNuevo = estadoAnterior;

        if (estadoNuevo != estadoAnterior)
        {
            // Pasamos por el service para que dispare side-effects (cupones, mail).
            await _inscripcionService.UpdateEstadoAsync(inscripcionId, estadoNuevo, ActorValidacion);
            _logger.LogInformation(
                "Inscripcion {Id}: {Old} → {New} (sum={Sum}, precioFinal={Pf}, montoReserva={Mr}, nuevos={Nuevos})",
                inscripcionId, estadoAnterior, estadoNuevo, sumApproved, precioFinal, montoReserva, pagosNuevos);
        }

        return new ValidacionInscripcionResult
        {
            InscripcionId = inscripcionId,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            MontoAprobado = sumApproved,
            PagosEncontrados = mpPagos.Count,
            PagosNuevos = pagosNuevos,
        };
    }

    public async Task<ValidacionBatchResult> ValidarPendientesPorDocumentoAsync(string documento)
    {
        var pendientes = await _inscripcionRepository.GetPendientesByDocumentoAsync(documento, null);
        var detalles = new List<ValidacionInscripcionResult>();
        var actualizadas = 0;

        foreach (var insc in pendientes)
        {
            if (insc.Estado != "Pendiente" && insc.Estado != "Reservada") continue;
            try
            {
                var r = await ValidarInscripcionAsync(insc.Id);
                if (r.Cambio) actualizadas++;
                detalles.Add(r);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validando inscripcion {Id}", insc.Id);
            }
        }

        return new ValidacionBatchResult
        {
            Revisadas = detalles.Count,
            Actualizadas = actualizadas,
            Detalles = detalles,
        };
    }

    private static string MapEstadoMP(string? mpStatus) => mpStatus switch
    {
        "approved" => EstadoAprobado,
        "pending" or "in_process" or "authorized" => EstadoPendiente,
        _ => EstadoRechazado,
    };
}
