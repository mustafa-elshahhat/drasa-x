using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using DerasaX.Api.Errors;
using DerasaX.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DerasaX.Api.RateLimiting
{
    /// <summary>
    /// Phase 5 cross-cutting rate limiting. Registers per-area fixed-window policies
    /// partitioned by the trusted identity (tenant + user) or, for anonymous auth
    /// endpoints, by client IP. Rejections return the canonical RFC 9457
    /// <c>429 Too Many Requests</c> problem document with a <c>Retry-After</c> header.
    ///
    /// Limits are resolved per-request through <see cref="IOptionsMonitor{TOptions}"/> so
    /// that test/dev hosts can override them via configuration (the override is only
    /// merged after the host is built, which is why the values are not captured eagerly).
    /// </summary>
    public static class RateLimitingExtensions
    {
        // Production-sensible defaults, applied when the config section omits a policy.
        private static readonly (int permit, int window) AuthDefault = (10, 60);
        private static readonly (int permit, int window) AiDefault = (30, 60);
        private static readonly (int permit, int window) SubmissionDefault = (30, 60);
        private static readonly (int permit, int window) MessagingDefault = (40, 60);
        private static readonly (int permit, int window) FilesDefault = (40, 60);
        private static readonly (int permit, int window) ReportsDefault = (20, 60);

        public static void AddDerasaXRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Anonymous auth surface → partition by client IP (brute-force protection).
                AddPolicy(options, RateLimitPolicies.Auth, AuthDefault, identityPartitioned: false);

                // Authenticated surfaces → partition by tenant+user so quotas are isolated.
                AddPolicy(options, RateLimitPolicies.Ai, AiDefault, identityPartitioned: true);
                AddPolicy(options, RateLimitPolicies.Submission, SubmissionDefault, identityPartitioned: true);
                AddPolicy(options, RateLimitPolicies.Messaging, MessagingDefault, identityPartitioned: true);
                AddPolicy(options, RateLimitPolicies.Files, FilesDefault, identityPartitioned: true);
                AddPolicy(options, RateLimitPolicies.Reports, ReportsDefault, identityPartitioned: true);

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var http = context.HttpContext;

                    TimeSpan? retryAfter = null;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra))
                        retryAfter = ra;

                    if (retryAfter is { } wait)
                        http.Response.Headers.RetryAfter =
                            ((int)Math.Ceiling(wait.TotalSeconds)).ToString(NumberFormatInfo.InvariantInfo);

                    if (http.Response.HasStarted)
                        return;

                    var pd = ProblemResultFactory.Build(
                        http,
                        StatusCodes.Status429TooManyRequests,
                        ErrorCodes.RateLimited,
                        "Too many requests.",
                        "Rate limit exceeded. Please retry after the indicated delay.",
                        retryable: true);

                    if (retryAfter is { } w)
                        pd.Extensions["retryAfterSeconds"] = (int)Math.Ceiling(w.TotalSeconds);

                    http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await http.Response.WriteAsJsonAsync(pd, pd.GetType(),
                        options: (JsonSerializerOptions?)null,
                        contentType: "application/problem+json", cancellationToken);
                };
            });
        }

        private static void AddPolicy(RateLimiterOptions options, string policyName,
            (int permit, int window) def, bool identityPartitioned)
        {
            options.AddPolicy(policyName, context =>
            {
                var cfg = context.RequestServices
                    .GetRequiredService<IOptionsMonitor<RateLimitingOptions>>().CurrentValue
                    .For(policyName, def.permit, def.window);

                var key = identityPartitioned
                    ? $"{policyName}:{IdentityKey(context)}"
                    : $"{policyName}:ip:{ClientIp(context)}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = cfg.PermitLimit,
                        Window = TimeSpan.FromSeconds(cfg.WindowSeconds),
                        QueueLimit = cfg.QueueLimit,
                        AutoReplenishment = true
                    });
            });
        }

        /// <summary>
        /// Partition key derived ONLY from trusted token claims: tenant + user. This guarantees
        /// one tenant cannot exhaust another tenant's quota, and one user cannot exhaust another's.
        /// Falls back to client IP for the (rare) authenticated-but-claimless request.
        /// </summary>
        private static string IdentityKey(HttpContext context)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var tenant = user.FindFirst("tenantId")?.Value;
                var uid = user.FindFirst("uid")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(uid))
                    return string.IsNullOrWhiteSpace(tenant) ? $"u:{uid}" : $"t:{tenant}:u:{uid}";
            }
            return $"ip:{ClientIp(context)}";
        }

        private static string ClientIp(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
