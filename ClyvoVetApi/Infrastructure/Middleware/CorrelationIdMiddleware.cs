using Serilog.Context;

namespace ClyvoVetApi.Infrastructure.Middleware
{
    /// <summary>
    /// Garante que toda requisição tenha um Correlation Id: reaproveita o enviado pelo
    /// cliente no header "X-Correlation-Id" ou gera um novo, devolve-o no header de resposta
    /// e o injeta no contexto de log estruturado (Serilog) e no trace atual (OpenTelemetry),
    /// permitindo seguir uma requisição do início ao fim em todas as camadas.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue) &&
                                 !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

            context.Response.Headers[HeaderName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                System.Diagnostics.Activity.Current?.SetTag("correlation.id", correlationId);
                await _next(context);
            }
        }
    }
}
