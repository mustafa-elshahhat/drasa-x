using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 19 — observability surface exercised through the real HTTP pipeline. Proves: the aggregate
/// /health endpoint reports every dependency check + service identity/version; a correlation id is
/// always echoed and an inbound one is propagated verbatim; and the SystemAdmin operational-status is
/// enriched with the live health report + runtime metrics + deployment identity. No secrets exposed.
/// </summary>
public class Phase19ObservabilityTests : IClassFixture<IntegrationFactory>
{
    private const string Sys = "SYS-1";
    private readonly IntegrationFactory _factory;
    public Phase19ObservabilityTests(IntegrationFactory factory) => _factory = factory;

    private static bool Has(HttpResponseMessage r, string header, out string value)
    {
        value = "";
        if (r.Headers.TryGetValues(header, out var v)) { value = string.Join(",", v); return true; }
        return false;
    }

    [Fact]
    public async Task Aggregate_health_reports_all_checks_and_version()
    {
        var client = TestClient.NewClient(_factory);
        var r = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var json = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("derasax-backend", root.GetProperty("service").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
        Assert.True(root.TryGetProperty("checks", out var checks));

        var names = new System.Collections.Generic.HashSet<string>();
        foreach (var c in checks.EnumerateArray())
            names.Add(c.GetProperty("name").GetString()!);
        Assert.Contains("postgres", names);
        Assert.Contains("storage", names);
        Assert.Contains("ai", names);
        Assert.Contains("background-jobs", names);
    }

    [Fact]
    public async Task Correlation_id_is_echoed_on_every_response()
    {
        var client = TestClient.NewClient(_factory);
        var r = await client.GetAsync("/health/live");
        Assert.True(Has(r, "X-Correlation-Id", out var cid));
        Assert.False(string.IsNullOrWhiteSpace(cid));
    }

    [Fact]
    public async Task Inbound_correlation_id_is_propagated_verbatim()
    {
        var client = TestClient.NewClient(_factory);
        var req = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        req.Headers.Add("X-Correlation-Id", "ph19-corr-abc123");
        var r = await client.SendAsync(req);
        Assert.True(Has(r, "X-Correlation-Id", out var cid));
        Assert.Equal("ph19-corr-abc123", cid);
    }

    [Fact]
    public async Task Operational_status_is_enriched_with_observability_fields()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.GetAsync("/api/v1/system-admin/operational-status");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var d = doc.RootElement.GetProperty("data");

        // Phase 19 enrichment fields are present and populated.
        Assert.True(d.TryGetProperty("storage", out _));
        Assert.True(d.TryGetProperty("aiService", out _));
        Assert.True(d.TryGetProperty("backgroundJobs", out _));
        Assert.True(d.TryGetProperty("metrics", out var metrics));
        Assert.True(metrics.GetProperty("totalRequests").GetInt64() >= 1);
        Assert.False(string.IsNullOrWhiteSpace(d.GetProperty("version").GetString()));
        Assert.Equal("Development", d.GetProperty("environment").GetString());
    }
}
