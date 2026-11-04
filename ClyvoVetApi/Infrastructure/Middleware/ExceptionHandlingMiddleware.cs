using System.Net;
using System.Text.Json;
using ClyvoVetApi.Application.Exceptions;
using ClyvoVetApi.DTOs;

namespace ClyvoVetApi.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware global de tratamento de exceções: converte exceções de negócio em
    /// respostas HTTP padronizadas (404 / 400) e qualquer outra exceção não prevista em 500,
    /// sempre registrando o erro com o Correlation Id da requisição.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = context.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value)
                    ? value.ToString()
                    : context.TraceIdentifier;

                var (statusCode, message) = ex switch
                {
                    NotFoundException => (HttpStatusCode.NotFound, ex.Message),
                    BusinessRuleException => (HttpStatusCode.BadRequest, ex.Message),
                    UnauthorizedAppException => (HttpStatusCode.Unauthorized, ex.Message),
                    ForbiddenAppException => (HttpStatusCode.Forbidden, ex.Message),
                    _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado ao processar a requisição.")
                };

                if (statusCode == HttpStatusCode.InternalServerError)
                    _logger.LogError(ex, "Erro não tratado. CorrelationId: {CorrelationId}", correlationId);
                else
                    _logger.LogWarning("Erro de negócio: {Message}. CorrelationId: {CorrelationId}", ex.Message, correlationId);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)statusCode;

                var body = new ApiErrorResponse
                {
                    Message = message,
                    CorrelationId = correlationId
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(body));
            }
        }
    }
}
