using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DerasaX.Tests;

/// <summary>Phase 18 — host with the deterministic EICAR test virus scanner enabled.</summary>
public class ScannerStubFactory : IntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Scanner:Mode"] = "Stub"
            }));
    }
}

/// <summary>
/// Phase 18 — host whose scanner is enabled but cannot produce a verdict. With the default
/// RejectOnUnavailable=false the upload succeeds and is honestly recorded ScannerUnavailable.
/// </summary>
public class ScannerUnavailableFactory : IntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Scanner:Mode"] = "Unavailable",
                ["FileStorage:Scanner:RejectOnUnavailable"] = "false"
            }));
    }
}

/// <summary>
/// Phase 22 PR-1 — host whose scanner cannot produce a verdict AND fails closed
/// (RejectOnUnavailable=true): an upload must be rejected (503), not recorded as anything.
/// </summary>
public class ScannerRejectsWhenUnavailableFactory : IntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Scanner:Mode"] = "Unavailable",
                ["FileStorage:Scanner:RejectOnUnavailable"] = "true"
            }));
    }
}

/// <summary>Phase 18 — host with security headers turned OFF (proves the config switch + source).</summary>
public class SecurityHeadersDisabledFactory : IntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SecurityHeaders:Enabled"] = "false"
            }));
    }
}
