using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClyvoVetApi.Infrastructure.HealthChecks
{
    /// <summary>
    /// Opções de configuração do <see cref="ExternalServiceHealthCheck"/>, lidas da seção
    /// "HealthChecks:ExternalService" do appsettings. Se a URL não for configurada, o check
    /// é ignorado (retorna Healthy) — útil em ambientes onde o serviço externo ainda não existe.
    /// </summary>
    public class ExternalServiceOptions
    {
        public string? Url { get; set; }
        public int TimeoutSeconds { get; set; } = 5;
    }

    /// <summary>
    /// Verifica a disponibilidade de um serviço externo do qual a API dependa (ex.: serviço
    /// de notificações, gateway de pagamento, provedor de CEP, etc.), usado pelo endpoint
    /// de readiness (<c>/health/ready</c>) exigido na Sprint 3.
    /// </summary>
    public class ExternalServiceHealthCheck : IHealthCheck
    {
        public const string HttpClientName = "ExternalServiceHealthCheck";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ExternalServiceOptions _options;

        public ExternalServiceHealthCheck(IHttpClientFactory httpClientFactory, ExternalServiceOptions options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Url))
            {
                return HealthCheckResult.Healthy("Nenhum serviço externo configurado para monitoramento.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient(HttpClientName);
                client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

                var response = await client.GetAsync(_options.Url, cancellationToken);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy($"Serviço externo respondeu com {(int)response.StatusCode}.")
                    : HealthCheckResult.Degraded($"Serviço externo respondeu com {(int)response.StatusCode}.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded("Falha ao contatar o serviço externo.", ex);
            }
        }
    }
}
