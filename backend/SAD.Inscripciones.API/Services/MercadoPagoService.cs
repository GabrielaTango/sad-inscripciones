using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.Preference;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class MercadoPagoService : IMercadoPagoService
{
    private readonly string _frontendBaseUrl;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(IConfiguration configuration, ILogger<MercadoPagoService> logger)
    {
        _logger = logger;
        var mpConfig = configuration.GetSection("MercadoPago");
        var accessToken = mpConfig["AccessToken"]!;
        MercadoPagoConfig.AccessToken = accessToken;
        _frontendBaseUrl = mpConfig["FrontendBaseUrl"] ?? "http://localhost:5173";
        _logger.LogInformation("MercadoPago inicializado con token: {TokenPrefix}...", accessToken[..Math.Min(15, accessToken.Length)]);
    }

    public async Task<MercadoPagoPreferenceResult> CrearPreferenciaAsync(Inscripcion inscripcion, string eventoTitulo, int cuotas = 1, decimal? montoOverride = null)
    {
        var client = new PreferenceClient();

        var request = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
            {
                new PreferenceItemRequest
                {
                    Title = $"Inscripcion - {eventoTitulo}",
                    Description = $"{inscripcion.Nombre} {inscripcion.Apellido}",
                    Quantity = 1,
                    CurrencyId = "ARS",
                    UnitPrice = montoOverride ?? inscripcion.PrecioFinal,
                }
            },
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = $"{_frontendBaseUrl}/pago/resultado?status=approved",
                Failure = $"{_frontendBaseUrl}/pago/resultado?status=rejected",
                Pending = $"{_frontendBaseUrl}/pago/resultado?status=pending",
            },
            PaymentMethods = new PreferencePaymentMethodsRequest
            {
                Installments = cuotas,
            },
            AutoReturn = "approved",
            ExternalReference = inscripcion.Id.ToString(),
        };

        _logger.LogInformation("Creando preferencia MP para inscripcion {Id}, monto {Monto} ARS",
            inscripcion.Id, inscripcion.PrecioFinal);

        Preference preference = await client.CreateAsync(request);

        _logger.LogInformation("Preferencia MP creada: Id={Id}, InitPoint={InitPoint}, SandboxInitPoint={SandboxInitPoint}",
            preference.Id, preference.InitPoint, preference.SandboxInitPoint);

        // Usar SandboxInitPoint para testing, InitPoint para produccion
        var initPoint = preference.InitPoint;

        return new MercadoPagoPreferenceResult
        {
            PreferenceId = preference.Id!,
            InitPoint = initPoint!,
        };
    }

    public async Task<MercadoPagoPaymentInfo?> ObtenerInfoPagoAsync(long paymentId)
    {
        var client = new PaymentClient();
        Payment payment = await client.GetAsync(paymentId);

        if (payment == null)
            return null;

        return new MercadoPagoPaymentInfo
        {
            Id = payment.Id ?? 0,
            Status = payment.Status ?? string.Empty,
            StatusDetail = payment.StatusDetail ?? string.Empty,
            TransactionAmount = payment.TransactionAmount ?? 0,
            ExternalReference = payment.ExternalReference,
            PaymentMethodId = payment.PaymentMethodId,
        };
    }

    public async Task<MercadoPagoPaymentInfo?> BuscarPagoPorReferenciaAsync(string externalReference)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", MercadoPagoConfig.AccessToken);

            var url = $"https://api.mercadopago.com/v1/payments/search?external_reference={externalReference}&sort=date_created&criteria=desc";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MP Search API respondio {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var results = json.GetProperty("results");

            if (results.GetArrayLength() == 0)
                return null;

            var payment = results[0];

            var info = new MercadoPagoPaymentInfo
            {
                Id = payment.GetProperty("id").GetInt64(),
                Status = payment.GetProperty("status").GetString() ?? string.Empty,
                StatusDetail = payment.GetProperty("status_detail").GetString() ?? string.Empty,
                TransactionAmount = payment.GetProperty("transaction_amount").GetDecimal(),
                ExternalReference = payment.TryGetProperty("external_reference", out var extRef) ? extRef.GetString() : null,
                PaymentMethodId = payment.TryGetProperty("payment_method_id", out var pmId) ? pmId.GetString() : null,
            };

            _logger.LogInformation("MP BuscarPorReferencia: encontrado pago {Id} status={Status} para ref={Ref}",
                info.Id, info.Status, externalReference);

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al buscar pago por referencia {Ref}", externalReference);
            return null;
        }
    }
}
