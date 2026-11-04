using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClyvoVetApi.Infrastructure.HealthChecks
{
    /// <summary>Verifica se o MongoDB responde ao comando <c>ping</c>.</summary>
    public class MongoDbHealthCheck : IHealthCheck
    {
        private readonly IMongoClient _client;

        public MongoDbHealthCheck(IMongoClient client)
        {
            _client = client;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                await _client.GetDatabase("admin")
                    .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cts.Token);

                return HealthCheckResult.Healthy("MongoDB respondeu ao ping.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("MongoDB indisponível.", ex);
            }
        }
    }
}