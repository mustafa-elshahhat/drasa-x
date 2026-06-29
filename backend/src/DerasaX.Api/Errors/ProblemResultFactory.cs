using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Errors
{
    /// <summary>
    /// Builds RFC 9457 Problem Details responses extended with the canonical
    /// <c>errorCode</c> and <c>correlationId</c> fields (Phase 2 ERROR_CONTRACT).
    /// The shape is identical for controllers and the global exception handler.
    /// </summary>
    public static class ProblemResultFactory
    {
        public static ProblemDetails Build(HttpContext http, int status, string errorCode, string title, string? detail = null, bool retryable = false)
        {
            var correlationId = GetCorrelationId(http);
            var pd = new ProblemDetails
            {
                Type = $"https://docs.derasax/errors/{errorCode.ToLowerInvariant().Replace('_', '-')}",
                Title = title,
                Status = status,
                Detail = detail,
                Instance = http.Request.Path
            };
            pd.Extensions["errorCode"] = errorCode;
            pd.Extensions["correlationId"] = correlationId;
            pd.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");
            pd.Extensions["retryable"] = retryable;
            return pd;
        }

        public static IActionResult Result(HttpContext http, int status, string errorCode, string title, string? detail = null, bool retryable = false)
        {
            var pd = Build(http, status, errorCode, title, detail, retryable);
            return new ObjectResult(pd)
            {
                StatusCode = status,
                ContentTypes = { "application/problem+json" }
            };
        }

        public static string GetCorrelationId(HttpContext http)
        {
            const string header = "X-Correlation-Id";
            // Phase 19 — the CorrelationIdMiddleware resolves + sanitizes the id once per request
            // and stores it here, so the error body and the response header always agree.
            if (http.Items.TryGetValue("CorrelationId", out var item) && item is string cid && !string.IsNullOrEmpty(cid))
            {
                http.Response.Headers[header] = cid;
                return cid;
            }
            if (http.Request.Headers.TryGetValue(header, out var existing) && !string.IsNullOrEmpty(existing))
                return existing!;
            var id = http.TraceIdentifier;
            http.Response.Headers[header] = id;
            return id;
        }
    }
}
