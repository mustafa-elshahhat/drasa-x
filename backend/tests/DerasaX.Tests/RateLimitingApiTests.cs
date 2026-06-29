using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 cross-cutting rate limiting. Each test news up its own factory with tiny,
/// policy-specific limits so the in-memory partitions are isolated per test (the shared
/// suite factory disables rate limiting to avoid contention). These tests prove the
/// intended endpoints are actually protected — not merely that middleware is registered.
/// </summary>
public class RateLimitingApiTests
{
    /// <summary>A test host that re-enables rate limiting (the base factory disables it) with caller-supplied limits.</summary>
    private sealed class RateLimitFactory : IntegrationFactory
    {
        private readonly Dictionary<string, string?> _overrides;
        public RateLimitFactory(Dictionary<string, string?> overrides) => _overrides = overrides;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(_overrides));
        }
    }

    private static Dictionary<string, string?> Limits(string policy, int permit, int extraAuth = 50) => new()
    {
        ["RateLimiting:Enabled"] = "true",
        // Keep the auth (login) budget generous so AuthedClient logins don't trip it,
        // unless the test under exercise is itself the auth policy.
        ["RateLimiting:Policies:auth:PermitLimit"] = (policy == "auth" ? permit : extraAuth).ToString(),
        ["RateLimiting:Policies:auth:WindowSeconds"] = "60",
        [$"RateLimiting:Policies:{policy}:PermitLimit"] = permit.ToString(),
        [$"RateLimiting:Policies:{policy}:WindowSeconds"] = "60",
    };

    [Fact]
    public async Task Auth_endpoint_allows_within_limit_then_returns_429_with_problem_contract()
    {
        const int limit = 3;
        await using var factory = new RateLimitFactory(Limits("auth", limit));
        var client = TestClient.NewClient(factory);

        // Anonymous login attempts (unknown user → non-429); the limiter counts every request.
        for (var i = 0; i < limit; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/v1/account/login",
                new { UserID = $"NO-SUCH-USER-{System.Guid.NewGuid():N}", Password = "whatever123" });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
        }

        var rejected = await client.PostAsJsonAsync("/api/v1/account/login",
            new { UserID = $"NO-SUCH-USER-{System.Guid.NewGuid():N}", Password = "whatever123" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.RetryAfter is not null, "429 must carry a Retry-After header.");

        using var doc = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal(429, root.GetProperty("status").GetInt32());
        Assert.Equal("RATE_LIMITED", root.GetProperty("errorCode").GetString());
        Assert.True(root.GetProperty("retryable").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task Identity_partitioned_endpoint_returns_429_after_limit()
    {
        const int limit = 3;
        await using var factory = new RateLimitFactory(Limits("messaging", limit));
        var client = await TestClient.AuthedClientAsync(factory, "TEACH-T1");

        for (var i = 0; i < limit; i++)
        {
            var ok = await client.GetAsync("/api/v1/conversations");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
        }

        var rejected = await client.GetAsync("/api/v1/conversations");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("RATE_LIMITED",
            JsonDocument.Parse(await rejected.Content.ReadAsStringAsync()).RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Quota_is_isolated_across_tenants_and_users()
    {
        const int limit = 3;
        await using var factory = new RateLimitFactory(Limits("messaging", limit));

        // Tenant-1 teacher exhausts their own messaging budget.
        var t1 = await TestClient.AuthedClientAsync(factory, "TEACH-T1");
        HttpResponseMessage last = null!;
        for (var i = 0; i <= limit; i++)
            last = await t1.GetAsync("/api/v1/conversations");
        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);

        // A different user in a DIFFERENT tenant is unaffected — proves the partition key
        // includes tenant+user, so one tenant cannot consume another tenant's quota.
        var t2 = await TestClient.AuthedClientAsync(factory, "TEACH-T2");
        var other = await t2.GetAsync("/api/v1/conversations");
        Assert.NotEqual(HttpStatusCode.TooManyRequests, other.StatusCode);
    }
}
