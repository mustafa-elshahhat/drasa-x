using DerasaX.Application.Extensions;
using DerasaX.Application.Services;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Notification;
using DerasaX.Application.Services.Notification;
using System.Linq;
using DerasaX.Api.Realtime;
using DerasaX.Api.Security;
using DerasaX.Api.Observability;
using DerasaX.Api.Observability.HealthChecks;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Operations;
using DerasaX.Domain.Entities.Models;
using DerasaX.Infrastructure.DbHelper.Context;
using DerasaX.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using DerasaX.Api.SeedData;


namespace DerasaX.Api.Helper
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Named CORS policy applied for the local frontends. Origins are read from
        /// configuration (<c>Cors:AllowedOrigins</c>), with no production defaults.
        /// </summary>
        public const string LocalCorsPolicy = "DerasaXLocalCors";

        public static IServiceCollection AddAllServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddInfrastructureServices(configuration);
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                    {
                        // Account lockout / brute-force protection (Phase 3 §14).
                        options.Lockout.AllowedForNewUsers = true;
                        options.Lockout.MaxFailedAccessAttempts =
                            configuration.GetValue("Auth:Lockout:MaxFailedAttempts", 5);
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                            configuration.GetValue("Auth:Lockout:LockoutMinutes", 15));

                        // Password policy.
                        options.Password.RequiredLength =
                            configuration.GetValue("Auth:Password:RequiredLength", 8);
                        options.Password.RequireDigit = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireNonAlphanumeric = false;

                        options.User.RequireUniqueEmail = false;
                    })
                    .AddEntityFrameworkStores<DerasaXDbContext>()
                    .AddDefaultTokenProviders();

            services.AddApplicationServices(configuration);
            services.AddAiServiceClient(configuration);
            services.AddScoped<IRealtimeSender, SignalRSender>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddSignalR();
            services.AddScoped<DataSeederService>();

            // Phase 19 — observability singletons (process-local request metrics + background-job heartbeat).
            services.AddSingleton<IRuntimeMetrics, RuntimeMetrics>();
            services.AddSingleton<IBackgroundJobHealth, BackgroundJobHealth>();

            services.AddLocalCors(configuration);
            services.AddHealthChecksServices();

            // Phase 18 — bind configurable HTTP security headers (SecurityHeaders section).
            services.Configure<SecurityHeadersOptions>(
                configuration.GetSection(SecurityHeadersOptions.SectionName));

            return services;
        }

        private static IServiceCollection AddLocalCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                 ?? Array.Empty<string>();

            // Phase 18 — fail fast on the dangerous wildcard-with-credentials misconfiguration.
            // CORS here always uses AllowCredentials, so a "*" origin would be both invalid
            // (browsers reject it) and a serious security hole. Require explicit origins.
            if (allowedOrigins.Any(o => string.Equals(o?.Trim(), "*", StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins must list explicit origins; '*' cannot be combined with credentials.");

            services.AddCors(options =>
            {
                options.AddPolicy(LocalCorsPolicy, policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials()
                              // Let the SPA read the correlation id for end-to-end tracing.
                              .WithExposedHeaders("X-Correlation-Id");
                    }
                });
            });

            return services;
        }

        private static IServiceCollection AddHealthChecksServices(this IServiceCollection services)
        {
            // Readiness gate ("ready" tag): PostgreSQL must be reachable. Liveness: process-only.
            // Phase 19 adds non-gating checks (storage / ai / background-jobs) so an operator can
            // see dependency health without those outages making the service report "not ready"
            // for unrelated traffic. The aggregate /health endpoint reports all of them.
            services.AddHealthChecks()
                    .AddDbContextCheck<DerasaXDbContext>(
                        name: "postgres",
                        tags: new[] { "ready" })
                    .AddTypeActivatedCheck<StorageHealthCheck>("storage", failureStatus: null, tags: new[] { "storage" })
                    .AddTypeActivatedCheck<AiServiceHealthCheck>("ai", failureStatus: null, tags: new[] { "ai" })
                    .AddTypeActivatedCheck<BackgroundJobsHealthCheck>("background-jobs", failureStatus: null, tags: new[] { "jobs" });

            return services;
        }

        /// <summary>
        /// Registers the typed HTTP client for the internal AI service (school-ai-rag).
        /// Base URL + timeout come from the <c>AiService</c> configuration section;
        /// no production URL or key is hardcoded.
        /// </summary>
        private static IServiceCollection AddAiServiceClient(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(DerasaX.Application.Common.AiServiceSettings.SectionName)
                               .Get<DerasaX.Application.Common.AiServiceSettings>()
                           ?? new DerasaX.Application.Common.AiServiceSettings();

            // Resilience (§15): the circuit breaker holds shared state across calls
            // and MUST be a singleton; the pipeline is stateless and reuses it.
            services.AddSingleton(new DerasaX.Application.Services.Ai.AiCircuitBreaker(settings.Resilience));
            services.AddSingleton(sp => new DerasaX.Application.Services.Ai.AiResiliencePipeline(
                settings.Resilience,
                sp.GetRequiredService<DerasaX.Application.Services.Ai.AiCircuitBreaker>()));

            services.AddHttpClient<
                DerasaX.Application.Services.Abstractions.Ai.IAiRagClient,
                DerasaX.Application.Services.Ai.AiRagClient>(client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds <= 0 ? 30 : settings.TimeoutSeconds);
            });

            return services;
        }
    }
}
