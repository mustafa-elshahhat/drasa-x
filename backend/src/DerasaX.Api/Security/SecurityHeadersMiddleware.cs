using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DerasaX.Api.Security
{
    /// <summary>
    /// Phase 18 — attaches HTTP security headers to every response (including error responses,
    /// because it runs early and uses <see cref="HttpResponse.OnStarting(Func{Task})"/>). Headers
    /// already set by a downstream handler are never overwritten. Configurable via
    /// <see cref="SecurityHeadersOptions"/>; HSTS is emitted only over HTTPS outside Development.
    /// </summary>
    public sealed class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityHeadersOptions _options;
        private readonly bool _isDevelopment;

        public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options, IWebHostEnvironment env)
        {
            _next = next;
            _options = options.Value;
            _isDevelopment = env.IsDevelopment();
        }

        public Task Invoke(HttpContext context)
        {
            if (!_options.Enabled)
                return _next(context);

            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                void Set(string name, string? value)
                {
                    if (!string.IsNullOrWhiteSpace(value) && !headers.ContainsKey(name))
                        headers[name] = value;
                }

                Set("X-Content-Type-Options", _options.ContentTypeOptions);
                Set("X-Frame-Options", _options.XFrameOptions);
                Set("Referrer-Policy", _options.ReferrerPolicy);
                Set("Permissions-Policy", _options.PermissionsPolicy);
                Set("Cross-Origin-Opener-Policy", _options.CrossOriginOpenerPolicy);
                Set("X-Permitted-Cross-Domain-Policies", "none");

                // CSP — applied to all responses except the Development Swagger UI (which needs
                // inline scripts/styles to render). The OpenAPI JSON itself is unaffected.
                var path = context.Request.Path.Value ?? string.Empty;
                var isSwaggerUi = _isDevelopment && path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
                if (!isSwaggerUi)
                    Set("Content-Security-Policy", _options.ContentSecurityPolicy);

                // HSTS only matters (and is only honoured) over HTTPS; never in Development.
                if (_options.EnableHsts && !_isDevelopment && context.Request.IsHttps)
                    Set("Strict-Transport-Security", _options.HstsValue);

                return Task.CompletedTask;
            });

            return _next(context);
        }
    }
}
