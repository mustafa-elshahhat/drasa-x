using DerasaX.Api.Helper;
using DerasaX.Api.Hubs;
using DerasaX.Api.RateLimiting;
using DerasaX.Api.SeedData;
using DerasaX.Domain.Entities.Models;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Phase 18 — do not advertise the server implementation (reduces fingerprinting).
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAllServices(builder.Configuration);

// Phase 5 cross-cutting rate limiting (config-driven; partitioned by tenant+user
// for authenticated surfaces and by client IP for the anonymous auth surface).
// Limits resolve per-request via IOptionsMonitor so test/dev hosts can override them.
builder.Services.AddDerasaXRateLimiting(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    // Phase 5 added DTOs whose short type names collide with legacy DTOs in other
    // namespaces (e.g. two distinct AddQuizDto). Use the full type name as the schema id
    // so Swagger generation never throws on a duplicate schemaId.
    c.CustomSchemaIds(t => t.FullName);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Local-development safety net: refuse to start against a known production
// database host. The committed Neon/Render production hosts must never be used
// from a developer machine. The start-local script performs the same guard.
GuardAgainstProductionDatabase(app);

// Phase 19 — fail-closed production configuration validation. Runs ONLY outside
// Development/Test, so local + integration tests are never affected. Blocks startup on
// placeholder/empty secrets, missing CORS origins, or an unmet system-admin MFA gate.
if (DerasaX.Api.Helper.ProductionConfigValidator.AppliesTo(app.Environment.EnvironmentName))
{
    var configErrors = DerasaX.Api.Helper.ProductionConfigValidator.Validate(
        app.Configuration, app.Environment.EnvironmentName);
    if (configErrors.Count > 0)
        throw new InvalidOperationException(
            "Refusing to start: production configuration is invalid:" + Environment.NewLine +
            " - " + string.Join(Environment.NewLine + " - ", configErrors));
}

// Configure the HTTP request pipeline.
// Swagger is exposed in Development only. The interactive UI can be disabled independently
// (Swagger:DisableUi) so the in-process integration test host need not load the unsigned
// SwaggerUI assembly; the JSON document remains available.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    if (!app.Configuration.GetValue<bool>("Swagger:DisableUi"))
        DerasaX.Api.Helper.SwaggerUiActivation.Enable(app);
}

// HTTPS redirection is unhelpful for local HTTP smoke tests / health probes,
// so it is only enforced outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Canonical Problem Details error handling (must wrap the pipeline early so it
// catches downstream exceptions and emits the RFC 9457 contract).
app.UseMiddleware<DerasaX.Api.Errors.ExceptionHandlingMiddleware>();

// Phase 19 — correlation id + structured request logging + request metrics. Placed
// just inside the error handler so EVERY request (including ones that later error) gets
// a correlation id pushed into the log scope and echoed on the response.
app.UseMiddleware<DerasaX.Api.Observability.CorrelationIdMiddleware>();

// Phase 18 — security response headers on every response (incl. error responses).
// Placed early (after error handling) so headers attach regardless of downstream outcome.
app.UseMiddleware<DerasaX.Api.Security.SecurityHeadersMiddleware>();

app.UseCors(DerasaX.Api.Helper.ServiceCollectionExtensions.LocalCorsPolicy);

// Explicit routing so the rate limiter can read each endpoint's [EnableRateLimiting]
// metadata (the matched endpoint must be selected before UseRateLimiter runs).
app.UseRouting();

app.UseAuthentication();

// Blocks every endpoint but a small allowlist (change-password/logout/revoke/refresh) for an
// authenticated user whose account still requires a forced password change. Runs after
// authentication (claims are validated) and before authorization (catches all controllers).
app.UseMiddleware<DerasaX.Api.Security.MustChangePasswordGateMiddleware>();

app.UseAuthorization();

// Rate limiter runs after authentication so policies can partition by the trusted
// tenant/user claims. Gated on the BUILT app configuration (which includes test/dev
// overrides); when disabled the policy attributes become no-ops, which is how the
// shared integration-test host avoids login/endpoint contention.
if (app.Configuration.GetValue("RateLimiting:Enabled", true))
    app.UseRateLimiter();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// -----------------------------------------------------------------------------
// Health endpoints
//   /health/live  -> process is up (no dependency checks)
//   /health/ready -> required dependencies (PostgreSQL) are reachable
// -----------------------------------------------------------------------------
// Health endpoints are the only deliberately anonymous endpoints (no sensitive
// data). AllowAnonymous opts them out of the secure-by-default fallback policy.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

// Phase 19 — aggregate, machine-readable health document (db + storage + ai +
// background-jobs + version + uptime). Anonymous-safe: statuses + safe descriptions
// only, never secrets/connection strings/tenant data. Operator-facing health surface.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = DerasaX.Api.Observability.HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();

// -----------------------------------------------------------------------------
// Development/Test-only data seeding.
//   * Never runs in Production.
//   * Gated behind the Seed:Enabled flag (default off; on in Development).
//   * The seeder itself is idempotent, so re-runs do not duplicate data.
// -----------------------------------------------------------------------------
var seedEnabled = app.Configuration.GetValue<bool>("Seed:Enabled");
if (seedEnabled && (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test")))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeederService>();

    // Path to the folder containing JSON files
    var seedDataPath = Path.Combine(app.Environment.ContentRootPath, "SeedData");

    await seeder.SeedAllAsync(seedDataPath);
}

app.Run();

static void GuardAgainstProductionDatabase(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
        return;

    var cs = app.Configuration.GetConnectionString("cs") ?? string.Empty;
    string[] blockedHosts = { "neon.tech", "render.com", "amazonaws.com" };

    foreach (var host in blockedHosts)
    {
        if (cs.Contains(host, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to start in Development against a non-local database host ('{host}'). " +
                "Local development must use the local PostgreSQL (derasax_local). " +
                "Set ConnectionStrings__cs to a local host or use appsettings.Development.json.");
        }
    }
}


// Exposes the implicit Program class to the integration test project (WebApplicationFactory<Program>).
public partial class Program { }
