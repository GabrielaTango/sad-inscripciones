using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.DTOs;
using SAD.Inscripciones.API.Exceptions;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class InscripcionService : IInscripcionService
{
    private readonly IInscripcionRepository _repository;
    private readonly IEventoRepository _eventoRepository;
    private readonly ITipoAlumnoRepository _tipoAlumnoRepository;
    private readonly IEventoPrecioRepository _eventoPrecioRepository;
    private readonly IEventoProvinciaBeneficioRepository _provinciaBeneficioRepository;
    private readonly IBecaCodigoRepository _becaCodigoRepository;
    private readonly IBecaEventoRepository _becaEventoRepository;
    private readonly IPromocionCuponRepository _promocionCuponRepository;
    private readonly IPromocionCuponService _promocionCuponService;
    private readonly DbConnectionFactory _dbFactory;

    public InscripcionService(
        IInscripcionRepository repository,
        IEventoRepository eventoRepository,
        ITipoAlumnoRepository tipoAlumnoRepository,
        IEventoPrecioRepository eventoPrecioRepository,
        IEventoProvinciaBeneficioRepository provinciaBeneficioRepository,
        IBecaCodigoRepository becaCodigoRepository,
        IBecaEventoRepository becaEventoRepository,
        IPromocionCuponRepository promocionCuponRepository,
        IPromocionCuponService promocionCuponService,
        DbConnectionFactory dbFactory)
    {
        _repository = repository;
        _eventoRepository = eventoRepository;
        _tipoAlumnoRepository = tipoAlumnoRepository;
        _eventoPrecioRepository = eventoPrecioRepository;
        _provinciaBeneficioRepository = provinciaBeneficioRepository;
        _becaCodigoRepository = becaCodigoRepository;
        _becaEventoRepository = becaEventoRepository;
        _promocionCuponRepository = promocionCuponRepository;
        _promocionCuponService = promocionCuponService;
        _dbFactory = dbFactory;
    }

    public async Task<IEnumerable<Inscripcion>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<Inscripcion> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id) ?? throw new NotFoundException("Inscripcion", id);
    }

    public async Task<IEnumerable<Inscripcion>> GetByEventoIdAsync(int eventoId) => await _repository.GetByEventoIdAsync(eventoId);

    public async Task<Inscripcion> CrearInscripcionAsync(InscripcionCreateDto dto, string createdBy)
    {
        var evento = await _eventoRepository.GetByIdAsync(dto.EventoId)
            ?? throw new BusinessException($"Evento con Id {dto.EventoId} no existe.");

        if (!evento.Activo)
            throw new BusinessException("El evento no está activo.");

        if (DateTime.UtcNow > evento.FechaCierreInscripcion)
            throw new BusinessException("El período de inscripción ha cerrado.");

        if (await _tipoAlumnoRepository.GetByIdAsync(dto.TipoAlumnoId) == null)
            throw new BusinessException($"TipoAlumno con Id {dto.TipoAlumnoId} no existe.");

        if (evento.MaxInscriptos.HasValue)
        {
            var count = await _repository.CountByEventoIdAsync(dto.EventoId);
            if (count >= evento.MaxInscriptos.Value)
                throw new BusinessException("El evento ha alcanzado el máximo de inscriptos.");
        }

        var eventoPrecio = await _eventoPrecioRepository.GetByEventoAndTipoAlumnoAsync(dto.EventoId, dto.TipoAlumnoId)
            ?? throw new BusinessException("No hay un precio configurado para este evento y tipo de alumno.");

        var precioBase = eventoPrecio.PrecioBase;
        decimal descuentoProvincia = 0;
        decimal descuentoBeca = 0;
        decimal descuentoCupon = 0;

        if (eventoPrecio.PermiteDescuento)
        {
            if (!string.IsNullOrEmpty(dto.Provincia))
            {
                var beneficio = await _provinciaBeneficioRepository.GetByEventoAndProvinciaAsync(dto.EventoId, dto.Provincia);
                if (beneficio != null && beneficio.Activo)
                {
                    descuentoProvincia = precioBase * beneficio.PorcentajeDescuento / 100m;
                }
            }

        }

        BecaCodigo? becaCodigo = null;
        BecaEvento? becaEvento = null;
        if (!string.IsNullOrEmpty(dto.CodigoBeca))
        {
            becaCodigo = await _becaCodigoRepository.GetByCodigoAsync(dto.CodigoBeca);
            if (becaCodigo == null)
                throw new BusinessException("El código de beca no es válido.");
            if (becaCodigo.Usado)
                throw new BusinessException("El código de beca ya fue utilizado.");

            becaEvento = await _becaEventoRepository.GetByIdAsync(becaCodigo.BecaEventoId);
            if (becaEvento == null || !becaEvento.Activo)
                throw new BusinessException("La campaña de beca no está activa.");
            if (becaEvento.EventoId != dto.EventoId)
                throw new BusinessException("El código de beca no corresponde a este evento.");
            if (becaEvento.FechaVencimiento.HasValue && DateTime.UtcNow > becaEvento.FechaVencimiento.Value)
                throw new BusinessException("El código de beca ha vencido.");

            if (becaEvento.TipoDescuento == "Porcentaje")
                descuentoBeca = precioBase * becaEvento.Valor / 100m;
            else
                descuentoBeca = becaEvento.Valor;

            if (!becaEvento.Acumulable)
            {
                // No acumulable: la beca reemplaza los otros descuentos si es mayor
                if (descuentoBeca >= descuentoProvincia)
                {
                    descuentoProvincia = 0;
                }
                else
                {
                    descuentoBeca = 0;
                }
            }
        }

        PromocionCupon? promocionCupon = null;
        if (!string.IsNullOrEmpty(dto.CodigoCupon))
        {
            promocionCupon = await _promocionCuponRepository.GetByCodigoAsync(dto.CodigoCupon);
            if (promocionCupon == null)
                throw new BusinessException("El código de cupón no es válido.");
            if (promocionCupon.Usado)
                throw new BusinessException("El cupón ya fue utilizado.");
            if (promocionCupon.FechaVencimiento.HasValue && DateTime.UtcNow > promocionCupon.FechaVencimiento.Value)
                throw new BusinessException("El cupón ha vencido.");
            if (promocionCupon.Documento != dto.Documento)
                throw new BusinessException("El cupón no corresponde a este documento.");

            if (promocionCupon.TipoDescuento == "Porcentaje")
                descuentoCupon = precioBase * promocionCupon.Valor / 100m;
            else
                descuentoCupon = promocionCupon.Valor;

            if (!promocionCupon.Acumulable)
            {
                var otrosDescuentos = descuentoProvincia + descuentoBeca;
                if (descuentoCupon >= otrosDescuentos)
                {
                    descuentoProvincia = 0;
                    descuentoBeca = 0;
                }
                else
                {
                    descuentoCupon = 0;
                }
            }
        }

        var descuentoTotal = descuentoProvincia + descuentoBeca + descuentoCupon;
        var precioFinal = Math.Max(0, precioBase - descuentoTotal);

        // Calcular precio en cuotas (misma lógica de descuento proporcional)
        decimal? precioFinalCuotas = null;
        int? cantidadCuotas = null;
        if (eventoPrecio.PrecioCuotas.HasValue && eventoPrecio.PrecioCuotas.Value > 0)
        {
            var descuentoPorcentaje = precioBase > 0 ? descuentoTotal / precioBase : 0;
            precioFinalCuotas = Math.Max(0, eventoPrecio.PrecioCuotas.Value - (eventoPrecio.PrecioCuotas.Value * descuentoPorcentaje));
            cantidadCuotas = eventoPrecio.CantidadCuotas;
        }

        // Calcular monto de reserva si corresponde
        decimal? montoReserva = null;
        if (dto.ModalidadPago == "reserva" && precioFinal > 0)
        {
            montoReserva = Math.Round(precioFinal * 0.3m, 0);
        }

        var inscripcion = new Inscripcion
        {
            EventoId = dto.EventoId,
            TipoAlumnoId = dto.TipoAlumnoId,
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Email = dto.Email,
            Telefono = dto.Telefono,
            Documento = dto.Documento,
            Provincia = dto.Provincia,
            PrecioBase = precioBase,
            DescuentoAplicado = descuentoTotal,
            PrecioFinal = precioFinal,
            PrecioFinalCuotas = precioFinalCuotas,
            CantidadCuotas = cantidadCuotas,
            MontoReserva = montoReserva,
            Estado = "Pendiente",
            FechaInscripcion = DateTime.UtcNow,
            FechaNacimiento = dto.FechaNacimiento,
            Domicilio = dto.Domicilio,
            CodigoPostal = dto.CodigoPostal,
            Localidad = dto.Localidad,
            Pais = dto.Pais,
            Celular = dto.Celular,
            Profesion = dto.Profesion,
            Especialidad = dto.Especialidad,
            Institucion = dto.Institucion,
            Sector = dto.Sector,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        using var connection = _dbFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var inscripcionId = await _repository.CreateAsync(inscripcion, connection, transaction);
            inscripcion.Id = inscripcionId;

            if (becaCodigo != null)
            {
                var marked = await _becaCodigoRepository.MarcarUsadoAsync(dto.CodigoBeca!, inscripcionId, connection, transaction);
                if (!marked)
                    throw new ConcurrencyException("No se pudo aplicar el código de beca. Puede que ya haya sido utilizado.");
            }

            if (promocionCupon != null)
            {
                var marked = await _promocionCuponRepository.MarcarUsadoAsync(dto.CodigoCupon!, inscripcionId, connection, transaction);
                if (!marked)
                    throw new ConcurrencyException("No se pudo aplicar el cupón. Puede que ya haya sido utilizado.");
            }

            transaction.Commit();
            return inscripcion;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateEstadoAsync(int id, string estado, string updatedBy)
    {
        var inscripcion = await GetByIdAsync(id);
        await _repository.UpdateEstadoAsync(id, estado, updatedBy);

        if (estado == "Confirmada")
        {
            await _promocionCuponService.GenerarCuponesParaInscripcionAsync(inscripcion);
        }
    }

    public async Task DeleteAsync(int id, string deletedBy)
    {
        await GetByIdAsync(id);
        await _repository.SoftDeleteAsync(id, deletedBy);
    }

    public async Task<IEnumerable<DTOs.InscripcionPendienteDto>> GetPendientesByDocumentoAsync(string documento, int? eventoId)
    {
        return await _repository.GetPendientesByDocumentoAsync(documento, eventoId);
    }
}
