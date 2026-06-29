using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 18 — security-hardening regression pack exercised through the real HTTP pipeline + live
/// PostgreSQL. Covers: HTTP security headers (present / on errors / config-gated), CORS origin
/// restriction (no wildcard reflection), the malware-scan abstraction (infected rejected / clean /
/// unavailable / default not-scanned), JSON output-encoding of injected HTML, invalid-enum
/// rejection, audit emission without credential leakage, and the header-Bearer (not ambient cookie)
/// auth model that keeps CSRF risk structurally low. Self-cleaning: deletes only its own rows.
/// </summary>
public class Phase18SecurityTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase18SecurityTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, string? message);
    private sealed record StoredFile(string id, string fileName, string contentType, long sizeBytes, string downloadUrl);
    private sealed record Signed(string token, DateTime expiresAtUtc, string downloadUrl);
    private sealed record IdRow(string id);

    // --- helpers -------------------------------------------------------------

    private static byte[] Pdf(int size = 64)
    {
        var b = new byte[size];
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
        Array.Copy(header, b, Math.Min(header.Length, size));
        return b;
    }

    // EICAR anti-malware test bytes, assembled from fragments so the literal signature never
    // appears contiguously in this source file (which would trip a developer's local antivirus).
    private static byte[] Eicar()
    {
        var s = string.Concat(@"X5O!P%@AP[4\PZX54(P^)7CC)7}$", "EICAR-STANDARD-", "ANTIVIRUS-TEST-FILE!", "$H+H*");
        return Encoding.ASCII.GetBytes(s);
    }

    private static MultipartFormDataContent Form(byte[] bytes, string fileName, string contentType, params (string, string)[] fields)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "File", fileName);
        foreach (var (k, v) in fields) form.Add(new StringContent(v), k);
        return form;
    }

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
            ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    private static async Task DeleteFileRows(IntegrationFactory factory, params string[] ids)
    {
        await using var db = Phase4Db.Platform(factory);
        foreach (var id in ids.Where(i => !string.IsNullOrEmpty(i)))
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", id);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"fileRecords\" WHERE \"Id\" = {0}", id);
        }
    }

    private static bool Has(HttpResponseMessage r, string header, out string value)
    {
        value = "";
        if (r.Headers.TryGetValues(header, out var v)) { value = string.Join(",", v); return true; }
        if (r.Content.Headers.TryGetValues(header, out var cv)) { value = string.Join(",", cv); return true; }
        return false;
    }

    // === Security headers ====================================================

    [Fact]
    public async Task Security_headers_present_on_api_response()
    {
        var client = TestClient.NewClient(_factory);
        var r = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        Assert.True(Has(r, "X-Content-Type-Options", out var cto)); Assert.Equal("nosniff", cto);
        Assert.True(Has(r, "X-Frame-Options", out var xfo)); Assert.Equal("DENY", xfo);
        Assert.True(Has(r, "Referrer-Policy", out var rp)); Assert.Equal("no-referrer", rp);
        Assert.True(Has(r, "Content-Security-Policy", out var csp)); Assert.Contains("frame-ancestors 'none'", csp);
        Assert.True(Has(r, "Permissions-Policy", out _));
        Assert.True(Has(r, "Cross-Origin-Opener-Policy", out var coop)); Assert.Equal("same-origin", coop);

        // In Development over HTTP, HSTS must NOT be emitted (browsers ignore it on http anyway,
        // and forcing it locally would break http smoke tests).
        Assert.False(Has(r, "Strict-Transport-Security", out _));
    }

    [Fact]
    public async Task Security_headers_present_on_unauthorized_response()
    {
        var client = TestClient.NewClient(_factory);
        var r = await client.GetAsync("/api/v1/audit"); // protected → 401
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.True(Has(r, "X-Content-Type-Options", out var cto)); Assert.Equal("nosniff", cto);
        Assert.True(Has(r, "Content-Security-Policy", out _));
    }

    [Fact]
    public async Task Security_headers_absent_when_disabled_by_config()
    {
        await using var off = new SecurityHeadersDisabledFactory();
        var client = TestClient.NewClient(off);
        var r = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.False(Has(r, "X-Content-Type-Options", out _));
        Assert.False(Has(r, "Content-Security-Policy", out _));
    }

    // === CORS ================================================================

    [Fact]
    public async Task Cors_reflects_configured_origin_with_credentials()
    {
        var client = TestClient.NewClient(_factory);
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/v1/account/login");
        req.Headers.Add("Origin", "http://localhost:5173"); // a Development-configured origin
        req.Headers.Add("Access-Control-Request-Method", "POST");
        req.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        var r = await client.SendAsync(req);

        Assert.True(Has(r, "Access-Control-Allow-Origin", out var acao));
        Assert.Equal("http://localhost:5173", acao);
        Assert.True(Has(r, "Access-Control-Allow-Credentials", out var acac));
        Assert.Equal("true", acac.ToLowerInvariant());
    }

    [Fact]
    public async Task Cors_does_not_reflect_untrusted_origin()
    {
        var client = TestClient.NewClient(_factory);
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/v1/account/login");
        req.Headers.Add("Origin", "http://evil.example");
        req.Headers.Add("Access-Control-Request-Method", "POST");
        var r = await client.SendAsync(req);

        // The untrusted origin must NOT be reflected (no wildcard, no echo).
        Assert.False(Has(r, "Access-Control-Allow-Origin", out _));
    }

    // === Virus scanning ======================================================

    [Fact]
    public async Task Infected_upload_is_rejected_by_stub_scanner()
    {
        await using var f = new ScannerStubFactory();
        var admin = await TestClient.AuthedClientAsync(f, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Eicar(), "notes.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("scan", (await resp.Content.ReadAsStringAsync()).ToLowerInvariant());
    }

    [Fact]
    public async Task Clean_upload_records_clean_scan_status_when_scanner_enabled()
    {
        await using var f = new ScannerStubFactory();
        var admin = await TestClient.AuthedClientAsync(f, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "clean.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var file = (await Read<StoredFile>(resp))!.data!;
        try
        {
            await using var db = Phase4Db.AsTenant(f, "tenant-1");
            var row = await db.fileRecords.FirstAsync(x => x.Id == file.id);
            Assert.Equal(DerasaX.Domain.Enums.FileScanStatus.Clean, row.ScanStatus);
        }
        finally { await DeleteFileRows(f, file.id); }
    }

    [Fact]
    public async Task Unavailable_scanner_records_scanner_unavailable_status()
    {
        await using var f = new ScannerUnavailableFactory();
        var admin = await TestClient.AuthedClientAsync(f, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "unscanned.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var file = (await Read<StoredFile>(resp))!.data!;
        try
        {
            await using var db = Phase4Db.AsTenant(f, "tenant-1");
            var row = await db.fileRecords.FirstAsync(x => x.Id == file.id);
            // Honest: scanner could not produce a verdict — NOT faked Clean.
            Assert.Equal(DerasaX.Domain.Enums.FileScanStatus.ScannerUnavailable, row.ScanStatus);
        }
        finally { await DeleteFileRows(f, file.id); }
    }

    [Fact]
    public async Task Unavailable_scanner_with_reject_policy_rejects_the_upload_503()
    {
        // Phase 22 PR-1 — fail-closed posture: when the scanner cannot produce a verdict AND
        // RejectOnUnavailable=true, the upload is rejected (503 Service Unavailable) and the bytes
        // are removed (no file row is created).
        await using var f = new ScannerRejectsWhenUnavailableFactory();
        var admin = await TestClient.AuthedClientAsync(f, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "rejected.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, resp.StatusCode);
    }

    [Fact]
    public async Task Default_host_records_not_scanned_status()
    {
        // Regression guard: the Phase 18 scanner default ("Disabled") preserves prior behaviour.
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "default.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var file = (await Read<StoredFile>(resp))!.data!;
        try
        {
            await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
            var row = await db.fileRecords.FirstAsync(x => x.Id == file.id);
            Assert.Equal(DerasaX.Domain.Enums.FileScanStatus.NotScanned, row.ScanStatus);
        }
        finally { await DeleteFileRows(_factory, file.id); }
    }

    // === Input validation / output encoding ==================================

    [Fact]
    public async Task Invalid_enum_purpose_is_rejected()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "x.pdf", "application/pdf", ("Purpose", "TotallyBogusPurpose")));
        // Model binding cannot bind an unknown enum → 400 (never a server crash / 500).
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Stored_html_is_served_as_json_data_not_executable_html()
    {
        var parent = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var (_, stuLogin) = await TestClient.LoginAsync(TestClient.NewClient(_factory), "STU-T1");
        var studentId = stuLogin!.id!;

        const string payload = "<script>alert('xss')</script>";
        var create = await parent.PostAsJsonAsync("/api/v1/parent-requests",
            new { studentId, type = 0, title = payload, body = "<img src=x onerror=alert(1)>" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var requestId = (await Read<IdRow>(create))!.data!.id;
        try
        {
            var get = await parent.GetAsync($"/api/v1/parent-requests/{requestId}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            // Stored-XSS defence for a JSON API: the user content is returned as JSON DATA, not an
            // HTML document. Two server-side controls neutralise the payload in any browser:
            //   1) the response is application/json (the browser parses it as data, never markup), and
            //   2) X-Content-Type-Options: nosniff prevents the browser sniffing it as text/html.
            // The frontend completes the defence by rendering as text (no dangerouslySetInnerHTML —
            // asserted in the frontend Phase 18 suite). The value round-trips intact (legitimate
            // Arabic/English/markup content is preserved, never lossily stripped).
            Assert.Equal("application/json", get.Content.Headers.ContentType?.MediaType);
            Assert.True(Has(get, "X-Content-Type-Options", out var cto)); Assert.Equal("nosniff", cto);

            var raw = await get.Content.ReadAsStringAsync();
            Assert.Contains("alert('xss')", raw); // preserved as data, not corrupted/stripped
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"parentRequestResponses\" WHERE \"ParentRequestId\" = {0}", requestId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"parentRequests\" WHERE \"Id\" = {0}", requestId);
        }
    }

    // === Audit & masking =====================================================

    [Fact]
    public async Task File_actions_emit_audit_without_leaking_signed_token()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var up = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "audited.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.Created, up.StatusCode);
        var file = (await Read<StoredFile>(up))!.data!;
        try
        {
            var issued = (await Read<Signed>(await admin.PostAsync($"/api/v1/files/{file.id}/signed-download", null)))!.data!;
            Assert.False(string.IsNullOrEmpty(issued.token));

            await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
            var rows = await db.Set<DerasaX.Domain.Entities.Models.AuditLog>()
                .Where(a => a.EntityType == "FileRecord" && a.EntityId == file.id)
                .ToListAsync();

            // Security-relevant actions were audited (create + signed-token issuance).
            Assert.True(rows.Count >= 1, "File upload / signed-token issuance must be audited.");

            // The signed-download token is a credential — it must NOT be persisted in the audit metadata.
            foreach (var row in rows)
                Assert.DoesNotContain(issued.token, row.MetadataJson ?? "");
        }
        finally { await DeleteFileRows(_factory, file.id); }
    }

    // === Auth model / CSRF posture ==========================================

    [Fact]
    public async Task Protected_endpoint_requires_bearer_not_ambient_cookie()
    {
        // Log in so the client holds whatever cookies the server sets (refreshToken).
        var client = TestClient.NewClient(_factory); // HandleCookies = true
        var (status, body) = await TestClient.LoginAsync(client, "ADMIN-T1");
        Assert.Equal(200, status);
        Assert.False(string.IsNullOrEmpty(body!.token));

        // Call a protected endpoint WITHOUT an Authorization header. The refresh cookie is
        // HttpOnly + path-scoped to /api/v1/account, so it cannot authenticate an API call:
        // a cross-site request riding the cookie alone is rejected. This is the CSRF defence.
        client.DefaultRequestHeaders.Authorization = null;
        var r = await client.GetAsync("/api/v1/audit");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Refresh_cookie_is_httponly_pathscoped_and_samesite()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.PostAsJsonAsync("/api/v1/account/login",
            new { UserID = "ADMIN-T1", Password = TestClient.Password });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        Assert.True(resp.Headers.TryGetValues("Set-Cookie", out var cookies));
        var refresh = cookies.FirstOrDefault(c => c.StartsWith("refreshToken", StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrEmpty(refresh));
        var lower = refresh!.ToLowerInvariant();
        Assert.Contains("httponly", lower);
        Assert.Contains("path=/api/v1/account", lower);
        Assert.Contains("samesite", lower);
        // Note: Secure is intentionally relaxed in Development (HTTP); it is enabled outside Development.
    }
}
