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
/// Route/RBAC audit (docs/audit/DERASAX_ROUTE_DETAIL_RBAC_AUDIT.md §8.1) — the legacy Phase-5
/// file metadata endpoints (<c>GET /api/v1/files</c>, <c>GET /files/{id}</c>,
/// <c>GET /files/{id}/metadata</c>, <c>POST /files/{id}/archive</c>) had no owner/role gate: any
/// tenant member (including a Student) could enumerate every tenant file's metadata (incl.
/// FileName/ContentType/StorageKey) and soft-archive any tenant file record.
///
/// This pack proves the fix mirrors the already-guarded Phase-16 patterns exactly:
///   - <c>FileStorageService.SoftDeleteAsync</c> (the guarded <c>DELETE /files/{id}</c>) — owner
///     OR SchoolAdmin/SystemAdmin may mutate a record; anyone else is 403 (mirrored by the new
///     <c>FileMetadataService.ArchiveAsync</c> owner-or-admin check).
///   - <c>FileStorageService.EnsureBaselineRead</c> (the guarded <c>GET /files/{id}/download</c>)
///     — owner, SchoolAdmin/SystemAdmin, or TenantInternal-visibility may read; anyone else is 403
///     (mirrored by the new <c>FileMetadataService.EnsureBaselineRead</c> and
///     <c>FileStorageService.GetMetadataAsync</c>'s now-added baseline-read check).
///   - List additionally restricts non-admins to files they own (they may not browse the full
///     tenant file list), consistent with the "administer tenant resources" role tier
///     (SchoolAdmin/SystemAdmin) used elsewhere in this codebase.
///
/// Same-tenant unauthorized access to an existing object returns 403 (not hidden); cross-tenant
/// access returns 404 (hidden), matching this codebase's established convention
/// (see TenantIsolationTests / Phase17RegressionTests.Private_file_is_readable_by_owner_and_admin_but_not_peer_or_other_tenant).
/// </summary>
public class FileMetadataAuthorizationTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public FileMetadataAuthorizationTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, string? message);
    private sealed record IdRow(string id);
    private sealed record StoredFile(string id, string fileName, string contentType, long sizeBytes, string downloadUrl);

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
            ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    /// <summary>Creates a file via the legacy JSON contract (Phase 5). This path never sets
    /// <c>Visibility</c> explicitly, so the record keeps the entity default —
    /// <see cref="DerasaX.Domain.Enums.FileVisibility.TenantInternal"/> — which, by the
    /// pre-existing, audited <c>EnsureBaselineRead</c> design, ANY tenant member may read (same
    /// rule the byte-download path already applies). Use this to prove list/archive scoping and
    /// the TenantInternal-read-vs-archive distinction; use <see cref="UploadPrivateFileAsync"/>
    /// when the test needs a peer-unreadable record.</summary>
    private async Task<string> CreateFileAsync(HttpClient owner, string fileName)
    {
        var resp = await owner.PostAsJsonAsync("/api/v1/files",
            new { fileName, contentType = "text/plain", sizeBytes = 128 });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await Read<IdRow>(resp))!.data!.id;
    }

    /// <summary>Creates a file via the Phase-16 durable-storage upload with
    /// <c>Purpose=Other</c>, whose default visibility is <c>Private</c> (owner/admin only — no
    /// TenantInternal bypass), matching Phase17RegressionTests' established "Other => Private"
    /// convention.</summary>
    private async Task<string> UploadPrivateFileAsync(HttpClient owner, string fileName)
    {
        var form = new MultipartFormDataContent();
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\nprivate-test-file");
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "File", fileName);
        form.Add(new StringContent("Other"), "Purpose");
        var resp = await owner.PostAsync("/api/v1/files/upload", form);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await Read<StoredFile>(resp))!.data!.id;
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

    private async Task<bool> IsArchivedAsync(string fileId)
    {
        await using var db = Phase4Db.Platform(_factory);
        var row = await db.fileRecords.IgnoreQueryFilters().FirstAsync(f => f.Id == fileId);
        return row.IsDeleted;
    }

    // ======================================================================
    // Owner regression guard — the happy path must keep working.
    // ======================================================================

    [Fact]
    public async Task Owner_can_list_get_metadata_and_archive_their_own_file()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var fileId = await CreateFileAsync(owner, "owner-happy-path.txt");
        try
        {
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/files/{fileId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/files/{fileId}/metadata")).StatusCode);

            var listBody = await (await owner.GetAsync("/api/v1/files")).Content.ReadAsStringAsync();
            Assert.Contains(fileId, listBody);

            Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync($"/api/v1/files/{fileId}/archive", null)).StatusCode);
            Assert.True(await IsArchivedAsync(fileId));
        }
        finally { await DeleteFileRows(fileId); }
    }

    // ======================================================================
    // The confirmed gap: a same-tenant, non-owning, non-admin peer must be blocked from a
    // Private-visibility file (the "another user's file" scenario the audit describes).
    // ======================================================================

    [Fact]
    public async Task Peer_cannot_get_or_read_metadata_or_archive_a_private_file_they_do_not_own()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var fileId = await UploadPrivateFileAsync(owner, "not-yours.pdf");
        try
        {
            // Same tenant, unrelated student (established seed "peer" identity — see
            // Phase17RegressionTests.Private_file_is_readable_by_owner_and_admin_but_not_peer_or_other_tenant).
            var peer = await TestClient.AuthedClientAsync(_factory, "PH8-OTHER-T1");
            Assert.Equal(HttpStatusCode.Forbidden, (await peer.GetAsync($"/api/v1/files/{fileId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await peer.GetAsync($"/api/v1/files/{fileId}/metadata")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await peer.PostAsync($"/api/v1/files/{fileId}/archive", null)).StatusCode);

            // The forbidden archive attempt must not have silently taken effect.
            Assert.False(await IsArchivedAsync(fileId));

            // Owner and tenant admin remain unaffected (regression guard alongside the gap proof).
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/files/{fileId}")).StatusCode);
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/files/{fileId}/metadata")).StatusCode);
        }
        finally { await DeleteFileRows(fileId); }
    }

    /// <summary>
    /// Nuance check: a TenantInternal-visibility file (the legacy create endpoint's default) is
    /// readable by any tenant member by the pre-existing, audited <c>EnsureBaselineRead</c> design
    /// (same bypass the byte-download path already grants — audit §12 item 6, "intended"). Archive
    /// is a mutation, not a read, and has no such bypass (mirrors <c>SoftDeleteAsync</c>): a peer
    /// may read but never archive someone else's file, TenantInternal or not.
    /// </summary>
    [Fact]
    public async Task Peer_can_read_a_tenantinternal_file_but_still_cannot_archive_it()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var fileId = await CreateFileAsync(owner, "shared-tenant-internal.txt");
        try
        {
            var peer = await TestClient.AuthedClientAsync(_factory, "PH8-OTHER-T1");
            Assert.Equal(HttpStatusCode.OK, (await peer.GetAsync($"/api/v1/files/{fileId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await peer.GetAsync($"/api/v1/files/{fileId}/metadata")).StatusCode);

            Assert.Equal(HttpStatusCode.Forbidden, (await peer.PostAsync($"/api/v1/files/{fileId}/archive", null)).StatusCode);
            Assert.False(await IsArchivedAsync(fileId));
        }
        finally { await DeleteFileRows(fileId); }
    }

    [Fact]
    public async Task Peer_list_does_not_enumerate_another_members_file_but_admin_list_does()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var fileId = await CreateFileAsync(owner, "enumeration-check.txt");
        try
        {
            var peer = await TestClient.AuthedClientAsync(_factory, "PH8-OTHER-T1");
            var peerListBody = await (await peer.GetAsync("/api/v1/files")).Content.ReadAsStringAsync();
            Assert.DoesNotContain(fileId, peerListBody);

            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            var adminListBody = await (await admin.GetAsync("/api/v1/files")).Content.ReadAsStringAsync();
            Assert.Contains(fileId, adminListBody);
        }
        finally { await DeleteFileRows(fileId); }
    }

    // ======================================================================
    // Tenant admin regression guard — SchoolAdmin keeps administering tenant files.
    // ======================================================================

    [Fact]
    public async Task SchoolAdmin_can_get_metadata_and_archive_a_tenant_members_file()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var fileId = await CreateFileAsync(owner, "admin-managed.txt");
        try
        {
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/files/{fileId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/files/{fileId}/metadata")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/files/{fileId}/archive", null)).StatusCode);
            Assert.True(await IsArchivedAsync(fileId));
        }
        finally { await DeleteFileRows(fileId); }
    }

    // ======================================================================
    // Cross-tenant existence stays hidden (404), never 403 — unchanged by this fix.
    // ======================================================================

    [Fact]
    public async Task CrossTenant_get_metadata_and_archive_still_return_404_not_403()
    {
        var owner = await TestClient.AuthedClientAsync(_factory, "STU-T1"); // tenant-1
        var fileId = await CreateFileAsync(owner, "cross-tenant.txt");
        try
        {
            var otherTenantAdmin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2"); // tenant-2
            Assert.Equal(HttpStatusCode.NotFound, (await otherTenantAdmin.GetAsync($"/api/v1/files/{fileId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await otherTenantAdmin.GetAsync($"/api/v1/files/{fileId}/metadata")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await otherTenantAdmin.PostAsync($"/api/v1/files/{fileId}/archive", null)).StatusCode);
        }
        finally { await DeleteFileRows(fileId); }
    }
}
