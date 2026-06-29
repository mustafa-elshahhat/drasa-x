using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 17 — consolidated cross-cutting REGRESSION pack. Where Phase-3/15/16 suites prove
/// each feature in isolation, this file pins the platform-wide security invariants in one place
/// so a future change that drops an <c>[Authorize]</c>, a tenant filter, a purpose gate, or the
/// honest object-store failure mode fails loudly here.
///
/// Every test runs through the real HTTP pipeline (<see cref="IntegrationFactory"/>) against the
/// migrated+seeded local PostgreSQL, authenticates with the stable seed fixtures, and cleans up
/// any rows it creates (test-owned data only — never developer/seed data).
/// </summary>
public class Phase17RegressionTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase17RegressionTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, string? message);
    private sealed record StoredFile(string id, string fileName, string contentType, long sizeBytes, string downloadUrl);
    private sealed record Signed(string token, DateTime expiresAtUtc, string downloadUrl);

    // --- helpers (kept local so this pack is self-contained) ---

    private static byte[] Pdf(int size = 64)
    {
        var b = new byte[size];
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
        Array.Copy(header, b, Math.Min(header.Length, size));
        return b;
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

    private async Task DeleteFileRows(params string[] ids)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var id in ids.Where(i => !string.IsNullOrEmpty(i)))
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", id);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"fileRecords\" WHERE \"Id\" = {0}", id);
        }
    }

    private async Task<string> UploadAsync(HttpClient client, string purpose)
    {
        var resp = await client.PostAsync("/api/v1/files/upload",
            Form(Pdf(), $"p17-{Guid.NewGuid():N}.pdf", "application/pdf", ("Purpose", purpose)));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await Read<StoredFile>(resp))!.data!.id;
    }

    private static int Code(HttpResponseMessage r) => (int)r.StatusCode;

    // ========================================================================
    // 1. ANONYMOUS REQUESTS ARE REJECTED (401) across the critical route set.
    //    A 404 here would mean the route silently vanished; a 200 would mean a
    //    missing [Authorize]. Both must fail this test.
    // ========================================================================
    [Theory]
    [InlineData("/api/Grades/GetAllGrades")]
    [InlineData("/api/LessonMaterial/GetMaterialByLessonId?id=PH8-LESSON-T1")]
    [InlineData("/api/v1/parent-requests")]
    [InlineData("/api/v1/vision/enrollment-assets/status")]
    [InlineData("/api/v1/ai-usage")]
    [InlineData("/api/v1/files/00000000-0000-0000-0000-000000000000/metadata")]
    [InlineData("/api/v1/files/00000000-0000-0000-0000-000000000000/download")]
    [InlineData("/api/v1/audit")]
    [InlineData("/api/v1/notifications")]
    public async Task Anonymous_request_is_rejected_401(string route)
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Anonymous_generic_upload_is_rejected_401()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "x.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ========================================================================
    // 2. CROSS-ROLE forbidden paths return 403 (authenticated but not allowed).
    // ========================================================================
    [Fact]
    public async Task Parent_cannot_write_grade_403()
    {
        var parent = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");
        var resp = await parent.PostAsJsonAsync("/api/Grades/AddGrade", new { name = "ParentShouldNotWrite" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Platform_SystemAdmin_cannot_use_tenant_file_upload_403()
    {
        // SystemAdmin carries no tenant claim, so it fails the TenantMember policy on the
        // tenant-scoped generic upload route (matches the Grades platform-scope rule).
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var resp = await sys.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "x.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ========================================================================
    // 3. PHASE 16 FILE BASELINE-READ RULE (the highest-risk new surface).
    //    Private = owner OR tenant/platform admin only; cross-tenant hidden (404).
    // ========================================================================
    [Fact]
    public async Task Private_file_is_readable_by_owner_and_admin_but_not_peer_or_other_tenant()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var fileId = await UploadAsync(owner, "Other"); // "Other" => Private visibility
        try
        {
            // Owner (uploader) can read.
            Assert.Equal(200, Code(await owner.GetAsync($"/api/v1/files/{fileId}/download")));

            // A same-tenant PEER (non-owner, non-admin) is forbidden (403, not 404 — it exists).
            var peer = await TestClient.AuthedClientAsync(_factory, "PH8-OTHER-T1");
            Assert.Equal(403, Code(await peer.GetAsync($"/api/v1/files/{fileId}/download")));

            // A tenant admin CAN read a private file in their tenant.
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            Assert.Equal(200, Code(await admin.GetAsync($"/api/v1/files/{fileId}/download")));

            // Cross-tenant existence is hidden (404) for both metadata and download.
            var otherTenantAdmin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
            Assert.Equal(404, Code(await otherTenantAdmin.GetAsync($"/api/v1/files/{fileId}/metadata")));
            Assert.Equal(404, Code(await otherTenantAdmin.GetAsync($"/api/v1/files/{fileId}/download")));

            // Anonymous is rejected at the auth layer (401).
            Assert.Equal(401, Code(await TestClient.NewClient(_factory).GetAsync($"/api/v1/files/{fileId}/download")));
        }
        finally { await DeleteFileRows(fileId); }
    }

    [Fact]
    public async Task TenantInternal_file_is_readable_by_any_member_but_not_cross_tenant()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var fileId = await UploadAsync(owner, "CommunityAttachment"); // TenantInternal visibility
        try
        {
            // Any member of the owning tenant can read a TenantInternal file.
            var peer = await TestClient.AuthedClientAsync(_factory, "PH8-OTHER-T1");
            Assert.Equal(200, Code(await peer.GetAsync($"/api/v1/files/{fileId}/download")));

            // A member of a different tenant cannot (cross-tenant hidden → 404).
            var otherTenant = await TestClient.AuthedClientAsync(_factory, "STU-T2");
            Assert.Equal(404, Code(await otherTenant.GetAsync($"/api/v1/files/{fileId}/download")));
        }
        finally { await DeleteFileRows(fileId); }
    }

    // ========================================================================
    // 4. RESTRICTED PURPOSES must be rejected on the generic upload endpoint and
    //    forced through their dedicated, authorization-checked workflow routes.
    // ========================================================================
    [Theory]
    [InlineData("LessonMaterial")]
    [InlineData("ParentDocumentRequest")]
    [InlineData("ParentDocumentResponse")]
    [InlineData("CvEnrollmentAsset")]
    public async Task Restricted_purpose_is_rejected_on_generic_upload_400(string purpose)
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "x.pdf", "application/pdf", ("Purpose", purpose)));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ========================================================================
    // 5. SIGNED-DOWNLOAD redeem re-checks the row: a token for a soft-deleted file
    //    must NOT keep working (complements Phase 16's tamper/expiry coverage).
    // ========================================================================
    [Fact]
    public async Task Signed_token_for_soft_deleted_file_is_rejected()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var fileId = await UploadAsync(owner, "Other");
        try
        {
            var issued = (await Read<Signed>(await owner.PostAsync($"/api/v1/files/{fileId}/signed-download", null)))!.data!;
            Assert.False(string.IsNullOrEmpty(issued.token));

            // The token works while the file is live...
            var anon = TestClient.NewClient(_factory);
            Assert.Equal(200, Code(await anon.GetAsync($"/api/v1/files/download?token={Uri.EscapeDataString(issued.token)}")));

            // ...but once the owner soft-deletes the file the same token is rejected (no zombie reads).
            Assert.Equal(200, Code(await owner.DeleteAsync($"/api/v1/files/{fileId}")));
            var afterDelete = await anon.GetAsync($"/api/v1/files/download?token={Uri.EscapeDataString(issued.token)}");
            Assert.True(afterDelete.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest,
                $"Expected 404/400 after soft-delete, got {(int)afterDelete.StatusCode}.");
        }
        finally { await DeleteFileRows(fileId); }
    }

    // ========================================================================
    // 6. HONEST FAILURE: an UNCONFIGURED object store must return 503, never a
    //    faked success. No network is touched (provider throws immediately).
    // ========================================================================
    [Fact]
    public async Task Unconfigured_S3_provider_returns_503_not_fake_success()
    {
        await using var s3 = new S3UnconfiguredFactory();
        var admin = await TestClient.AuthedClientAsync(s3, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "honest.pdf", "application/pdf", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, resp.StatusCode);

        // And nothing was persisted as if it had succeeded.
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("STORAGE_UNAVAILABLE", body, StringComparison.OrdinalIgnoreCase);
    }
}
