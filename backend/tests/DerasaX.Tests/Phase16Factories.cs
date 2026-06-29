using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DerasaX.Tests;

/// <summary>Phase 16 — host with the (default-OFF) CV enrollment asset capability enabled.</summary>
public class CvAssetsEnabledFactory : IntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["Cv:EnrollmentAssetsEnabled"] = "true" }));
    }
}

/// <summary>Phase 16 — host with a 1-second signed-download TTL so token expiry can be exercised.</summary>
public class ShortTokenTtlFactory : IntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:SignedUrlTtlSeconds"] = "1" }));
    }
}
