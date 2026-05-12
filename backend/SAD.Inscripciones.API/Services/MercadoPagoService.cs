using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.Preference;
using SAD.Inscripciones.API.Models;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services.Interfaces;

namespace SAD.Inscripciones.API.Services;

public class MercadoPagoService : IMercadoPagoService
{
    private static readonly TimeSpan ConfigCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICryptoService _crypto;
    private readonly ILogger<MercadoPagoService> _logger;
    private readonly string _fallbackFrontendBaseUrl;

    private string? _cachedAccessToken;
    private string? _cachedFrontendBaseUrl;
    private DateTime _cachedAt;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public MercadoPagoService(
        IServiceScopeFactory scopeFactory,
        ICryptoService crypto,
        IConfiguration configuration,
        ILogger<MercadoPagoService> logger)
    {
        _scopeFactory = scopeFactory;
        _crypto = crypto;
        _logger = logger;
        // Solo se usa si la DB no tiene FrontendBaseUrl seteado.
        _fallbackFrontendBaseUrl = configuration["MercadoPago:FrontendBaseUrl"] ?? "http://localhost:5173";
    }

    public async Task<string> EnsureConfiguradoAsync()
    {
        if (_cachedAccessToken != null && DateTime.UtcNow - _cachedAt < ConfigCacheTtl)
        {
            MercadoPagoConfig.AccessToken = _cachedAccessToken;
            return _cachedFrontendBaseUrl ?? _fallbackFrontendBaseUrl;
        }

        await _cacheLock.WaitAsync();
        try
        {
            if (_cachedAccessToken != null && DateTime.UtcNow - _cachedAt < ConfigCacheTtl)
            {
                MercadoPagoConfig.AccessToken = _cachedAccessToken;
                return _cachedFrontendBaseUrl ?? _fallbackFrontendBaseUrl;
            }

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IConfiguracionMercadoPagoRepository>();
            var config = await repo.GetAsync();

            var token = string.IsNullOrEmpty(config.AccessTokenCifrado)
                ? string.Empty
                : _crypto.Decrypt(config.AccessTokenCifrado);

            var frontendBaseUrl = string.IsNullOrWhiteSpace(config.FrontendBaseUrl)
                ? _fallbackFrontendBaseUrl
                : config.FrontendBaseUrl;

            _cachedAccessToken = token;
            _cachedFrontendBaseUrl = frontendBaseUrl;
            _cachedAt = DateTime.UtcNow;

            MercadoPagoConfig.AccessToken = token;

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("MercadoPago: AccessToken no configurado en la DB. Cargá uno desde /admin/configuracion-mercadopago.");
            }
            else
            {
                _logger.LogInformation("MercadoPago configurado con token {TokenPrefix}...",
                    token[..Math.Min(15, token.Length)]);
            }

            return frontendBaseUrl;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public void InvalidarCache()
    {
        _cachedAccessToken = null;
        _cachedFrontendBaseUrl = null;
    }

    public async Task<MercadoPagoPreferenceResult> CrearPreferenciaAsync(Inscripcion inscripcion, string eventoTitulo, int cuotas = 1, decimal? montoOverride = null)
    {
        var frontendBaseUrl = await EnsureConfiguradoAsync();
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
                Success = $"{frontendBaseUrl}/pago/resultado?status=approved",
                Failure = $"{frontendBaseUrl}/pago/resultado?status=rejected",
                Pending = $"{frontendBaseUrl}/pago/resultado?status=pending",
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
        await EnsureConfiguradoAsync();
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
            await EnsureConfiguradoAsync();
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

    public async Task<IReadOnlyList<MercadoPagoPaymentInfo>> BuscarTodosPagosPorReferenciaAsync(string externalReference)
    {
        var result = new List<MercadoPagoPaymentInfo>();
        try
        {
            await EnsureConfiguradoAsync();
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", MercadoPagoConfig.AccessToken);

            var url = $"https://api.mercadopago.com/v1/payments/search?external_reference={externalReference}&sort=date_created&criteria=asc";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MP Search API respondio {Status} para ref={Ref}", response.StatusCode, externalReference);
                return result;
            }

            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var results = json.GetProperty("results");

            for (var i = 0; i < results.GetArrayLength(); i++)
            {
                var p = results[i];
                result.Add(new MercadoPagoPaymentInfo
                {
                    Id = p.GetProperty("id").GetInt64(),
                    Status = p.GetProperty("status").GetString() ?? string.Empty,
                    StatusDetail = p.GetProperty("status_detail").GetString() ?? string.Empty,
                    TransactionAmount = p.GetProperty("transaction_amount").GetDecimal(),
                    ExternalReference = p.TryGetProperty("external_reference", out var extRef) ? extRef.GetString() : null,
                    PaymentMethodId = p.TryGetProperty("payment_method_id", out var pmId) ? pmId.GetString() : null,
                });
            }

            _logger.LogInformation("MP BuscarTodos: {Count} pagos para ref={Ref}", result.Count, externalReference);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al buscar pagos por referencia {Ref}", externalReference);
            return result;
        }
    }
}
