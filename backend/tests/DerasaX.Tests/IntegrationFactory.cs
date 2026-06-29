using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DerasaX.Tests;

/// <summary>
/// Boots the real DerasaX.Api in-process against the migrated+seeded local
/// PostgreSQL database. Seeding is disabled here (the database is already seeded
/// by the local environment); the tests rely on the stable Phase 3 fixture set.
/// </summary>
public class IntegrationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development so appsettings.Development.json (local DB + dev keys) loads
        // and Secure cookies are relaxed for the in-memory test client.
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Do not re-run the JSON seeder from the test content root.
                ["Seed:Enabled"] = "false",
                // The in-process test host does not exercise the /swagger UI; skipping it avoids loading
                // the (unsigned) Swashbuckle SwaggerUI assembly, which Smart App Control blocks on this
                // machine (0x800711C7). Pure test-host concern — production/dev behaviour is unchanged.
                ["Swagger:DisableUi"] = "true",
                // The shared suite runs class-parallel and hammers the seed logins/endpoints
                // from a single loopback client; rate limiting is therefore disabled here and
                // exercised explicitly by RateLimitingApiTests (its own factory + tiny limits).
                ["RateLimiting:Enabled"] = "false"
            });
        });
    }
}
