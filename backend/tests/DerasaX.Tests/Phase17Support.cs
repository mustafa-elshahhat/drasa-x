using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DerasaX.Tests;

/// <summary>
/// Phase 17 — host that selects the S3 durable provider WITHOUT any bucket/credentials
/// (the local/CI reality). Used to prove that an unconfigured object store fails honestly
/// (503 STORAGE_UNAVAILABLE) instead of faking a successful upload. No network is touched:
/// the S3 provider throws <c>StorageUnavailableException</c> the moment it is asked to act.
/// </summary>
public class S3UnconfiguredFactory : IntegrationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:Provider"] = "S3"
                // Deliberately NO FileStorage:S3 Bucket/AccessKeyId/SecretAccessKey — unconfigured.
            }));
    }
}
