using System;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DerasaX.Api.Errors
{
    /// <summary>
    /// Translates unhandled / domain exceptions into canonical Problem Details
    /// responses. Sensitive details (SQL, stack traces, secrets) never reach the
    /// client — the full exception is logged server-side keyed by correlationId.
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

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = ProblemResultFactory.GetCorrelationId(context);
                _logger.LogError(ex, "Unhandled exception. correlationId={CorrelationId}", correlationId);

                var (status, code, title) = ex switch
                {
                    BadRequestException => (StatusCodes.Status400BadRequest, ErrorCodes.BadRequest, "Bad request."),
                    NotFoundException => (StatusCodes.Status404NotFound, ErrorCodes.NotFound, "Resource not found."),
                    ConflictException => (StatusCodes.Status409Conflict, ErrorCodes.Conflict, "Conflict."),
                    PlanLimitExceededException => (StatusCodes.Status409Conflict, ErrorCodes.PlanLimitExceeded, "Plan limit exceeded."),
                    ForbiddenException => (StatusCodes.Status403Forbidden, ErrorCodes.Forbidden, "Forbidden."),
                    UnauthorizedException => (StatusCodes.Status401Unauthorized, ErrorCodes.Unauthenticated, "Unauthenticated."),
                    ImageValidationException => (StatusCodes.Status400BadRequest, ErrorCodes.ValidationError, "Validation failed."),
                    ValidationException => (StatusCodes.Status400BadRequest, ErrorCodes.ValidationError, "Validation failed."),
                    // AI orchestration failures: stable upstream error, never a fake success.
                    AiServiceException => (StatusCodes.Status502BadGateway, ErrorCodes.AiUnavailable, "AI service unavailable."),
                    // Durable storage provider unreachable/unconfigured: honest 503, never faked.
                    StorageUnavailableException => (StatusCodes.Status503ServiceUnavailable, ErrorCodes.StorageUnavailable, "File storage unavailable."),
                    _ => (StatusCodes.Status500InternalServerError, ErrorCodes.InternalError, "An unexpected error occurred.")
                };

                if (context.Response.HasStarted)
                    throw;

                // Only echo the exception message for the safe, intentional domain
                // exceptions; never for unhandled 500s.
                var detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message;
                var pd = ProblemResultFactory.Build(context, status, code, title, detail,
                    retryable: status >= 500);

                context.Response.Clear();
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(pd, pd.GetType());
            }
        }
    }
}
