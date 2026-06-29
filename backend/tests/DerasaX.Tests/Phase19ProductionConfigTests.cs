using System.Collections.Generic;
using System.Linq;
using DerasaX.Api.Helper;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 19 — fail-closed production configuration validation. Proves the validator is a no-op in
/// Development/Test (so local + integration runs are never affected) and blocks Production startup on
/// empty/placeholder secrets, missing CORS origins, and an unmet system-admin MFA gate. Pure unit tests
/// (no host) so they cannot disturb the running stack.
/// </summary>
public class Phase19ProductionConfigTests
{
    private const string Strong = "K7p2X9w4R1t8Y3u6N0m5Q8s2D4f7G1h3J6k9L2z5C8v1B4n7"; // 48 chars, no markers

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Development_is_never_enforced()
    {
        Assert.False(ProductionConfigValidator.AppliesTo("Development"));
        Assert.False(ProductionConfigValidator.AppliesTo("Test"));
        // Even with everything empty, Development returns no errors.
        var errors = ProductionConfigValidator.Validate(Config(new()), "Development");
        Assert.Empty(errors);
    }

    [Fact]
    public void Production_blocks_empty_secrets_and_missing_origins()
    {
        var errors = ProductionConfigValidator.Validate(Config(new()), "Production");
        Assert.Contains(errors, e => e.Contains("SecretKey"));
        Assert.Contains(errors, e => e.Contains("ServiceAuth:SigningKey"));
        Assert.Contains(errors, e => e.Contains("Cors:AllowedOrigins"));
    }

    [Fact]
    public void Production_blocks_dev_placeholder_secrets()
    {
        var values = new Dictionary<string, string?>
        {
            ["SecretKey"] = "local-dev-only-signing-key-not-for-production-0123456789abcdef",
            ["ServiceAuth:SigningKey"] = "local-dev-only-service-signing-key-not-for-production-abcdef0123456789",
            ["Cors:AllowedOrigins:0"] = "https://app.derasax.example"
        };
        var errors = ProductionConfigValidator.Validate(Config(values), "Production");
        Assert.Contains(errors, e => e.Contains("SecretKey") && e.Contains("placeholder"));
        Assert.Contains(errors, e => e.Contains("ServiceAuth:SigningKey") && e.Contains("placeholder"));
    }

    [Fact]
    public void Production_mfa_gate_blocks_when_required_but_unwired()
    {
        var values = new Dictionary<string, string?>
        {
            ["SecretKey"] = Strong,
            ["ServiceAuth:SigningKey"] = Strong,
            ["Cors:AllowedOrigins:0"] = "https://app.derasax.example",
            ["Auth:SystemAdminMfaRequired"] = "true"
            // Auth:MfaProvider intentionally absent
        };
        var errors = ProductionConfigValidator.Validate(Config(values), "Production");
        Assert.Contains(errors, e => e.Contains("MFA"));
    }

    [Fact]
    public void Production_passes_with_strong_secrets_origins_and_no_mfa_requirement()
    {
        var values = new Dictionary<string, string?>
        {
            ["SecretKey"] = Strong,
            ["ServiceAuth:SigningKey"] = Strong + "EXTRA",
            ["Cors:AllowedOrigins:0"] = "https://app.derasax.example",
            ["Auth:SystemAdminMfaRequired"] = "false"
        };
        var errors = ProductionConfigValidator.Validate(Config(values), "Production");
        Assert.Empty(errors);
    }

    [Theory] // Phase 22 PR-4 (BE-04/SEC-04) — the ACTUAL appsettings.Development local-dev keys are rejected in Production.
    [InlineData("SecretKey", "local-dev-only-signing-key-not-for-production-0123456789abcdef")]
    [InlineData("ServiceAuth:SigningKey", "local-dev-only-service-signing-key-not-for-production-abcdef0123456789")]
    [InlineData("FileStorage:SigningKey", "local-dev-only-file-download-signing-key-not-for-production-0123456789abcdef")]
    public void Production_rejects_each_literal_local_dev_only_key(string keyName, string localDevValue)
    {
        var values = new Dictionary<string, string?>
        {
            ["SecretKey"] = Strong,
            ["ServiceAuth:SigningKey"] = Strong + "X",
            ["Cors:AllowedOrigins:0"] = "https://app.derasax.example",
        };
        values[keyName] = localDevValue; // override the key under test with its real local-dev value
        var errors = ProductionConfigValidator.Validate(Config(values), "Production");
        Assert.Contains(errors, e => e.Contains(keyName) && e.Contains("placeholder"));
    }
}
