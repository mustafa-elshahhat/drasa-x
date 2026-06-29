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
/// Phase 16 — durable file storage through the real HTTP pipeline: validation (type/size/
/// traversal), tenant isolation, baseline + relationship authorization, signed-token download
/// (valid / tampered / expired), soft-delete blocking, sensitive-download audit, the lesson-
/// material and parent-document end-to-end flows, and the consent/tenant-scoped CV asset path.
/// </summary>
public class Phase16FileStorageApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase16FileStorageApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, string? message);
    private sealed record StoredFile(string id, string fileName, string contentType, long sizeBytes, string downloadUrl);
    private sealed record Signed(string token, DateTime expiresAtUtc, string downloadUrl);
    private sealed record Material(string id, string title, string url, string? fileRecordId);
    private sealed record IdRow(string id);
    private sealed record Enrollment(string id, string studentId);

    // --- helpers ---

    private static byte[] Pdf(int size = 64)
    {
        var b = new byte[size];
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
        Array.Copy(header, b, Math.Min(header.Length, size));
        return b;
    }
    private static byte[] Png(int size = 64)
    {
        var b = new byte[size];
        byte[] sig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Array.Copy(sig, b, Math.Min(sig.Length, size));
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

    // --- tests ---

    [Fact]
    public async Task Upload_valid_file_persists_metadata_in_postgres()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "syllabus.pdf", "application/pdf", ("Purpose", "CommunityAttachment")));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var file = (await Read<StoredFile>(resp))!.data!;
        Assert.False(string.IsNullOrEmpty(file.id));
        Assert.Equal("syllabus.pdf", file.fileName);

        // Durable metadata really persisted in PostgreSQL (not an in-memory/ephemeral row).
        await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
        {
            var row = await db.fileRecords.FirstOrDefaultAsync(f => f.Id == file.id);
            Assert.NotNull(row);
            Assert.Equal("tenant-1", row!.TenantId);
            Assert.False(string.IsNullOrEmpty(row.ChecksumSha256));
            Assert.Equal("Local", row.StorageProvider);
            Assert.Equal(DerasaX.Domain.Enums.FileScanStatus.NotScanned, row.ScanStatus);
        }
        await DeleteFileRows(file.id);
    }

    [Fact]
    public async Task Reject_invalid_file_type()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(new byte[] { 1, 2, 3 }, "malware.exe", "application/octet-stream", ("Purpose", "Other")));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reject_oversized_file()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        // ProfileImage cap is 5 MB; send 6 MB.
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Png(6 * 1024 * 1024), "huge.png", "image/png", ("Purpose", "ProfileImage")));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reject_restricted_purpose_on_generic_upload()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "secret.pdf", "application/pdf", ("Purpose", "ParentDocumentRequest")));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CrossTenant_metadata_and_download_blocked()
    {
        var admin1 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var up = await admin1.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "t1.pdf", "application/pdf", ("Purpose", "Other")));
        var file = (await Read<StoredFile>(up))!.data!;
        try
        {
            var admin2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await admin2.GetAsync($"/api/v1/files/{file.id}/metadata")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin2.GetAsync($"/api/v1/files/{file.id}/download")).StatusCode);
        }
        finally { await DeleteFileRows(file.id); }
    }

    [Fact]
    public async Task SignedToken_valid_and_tamper_rejected()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var up = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "signed.pdf", "application/pdf", ("Purpose", "Other")));
        var file = (await Read<StoredFile>(up))!.data!;
        try
        {
            var issued = (await Read<Signed>(await admin.PostAsync($"/api/v1/files/{file.id}/signed-download", null)))!.data!;
            Assert.False(string.IsNullOrEmpty(issued.token));

            // Anonymous client (no auth header) can redeem the signed token.
            var anon = TestClient.NewClient(_factory);
            var ok = await anon.GetAsync($"/api/v1/files/download?token={Uri.EscapeDataString(issued.token)}");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

            // A tampered token is rejected.
            var tampered = issued.token[..^2] + (issued.token[^1] == 'A' ? "BB" : "AA");
            var bad = await anon.GetAsync($"/api/v1/files/download?token={Uri.EscapeDataString(tampered)}");
            Assert.True(bad.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound);
        }
        finally { await DeleteFileRows(file.id); }
    }

    [Fact]
    public async Task SignedToken_expires()
    {
        await using var shortFactory = new ShortTokenTtlFactory();
        var admin = await TestClient.AuthedClientAsync(shortFactory, "ADMIN-T1");
        var up = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "exp.pdf", "application/pdf", ("Purpose", "Other")));
        var file = (await Read<StoredFile>(up))!.data!;
        try
        {
            var issued = (await Read<Signed>(await admin.PostAsync($"/api/v1/files/{file.id}/signed-download", null)))!.data!;
            await Task.Delay(1500); // TTL is 1 second in this host
            var anon = TestClient.NewClient(shortFactory);
            var expired = await anon.GetAsync($"/api/v1/files/download?token={Uri.EscapeDataString(issued.token)}");
            Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(shortFactory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", file.id);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"fileRecords\" WHERE \"Id\" = {0}", file.id);
        }
    }

    [Fact]
    public async Task SoftDelete_blocks_future_download()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var up = await admin.PostAsync("/api/v1/files/upload",
            Form(Pdf(), "doomed.pdf", "application/pdf", ("Purpose", "Other")));
        var file = (await Read<StoredFile>(up))!.data!;
        try
        {
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/files/{file.id}/download")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/v1/files/{file.id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/files/{file.id}/download")).StatusCode);
        }
        finally { await DeleteFileRows(file.id); }
    }

    [Fact]
    public async Task LessonMaterial_upload_by_teacher_download_by_student_tenant_scoped()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var up = await teacher.PostAsync("/api/LessonMaterial/UploadMaterial",
            Form(Pdf(), "lesson1.pdf", "application/pdf", ("LessonId", "PH8-LESSON-T1"), ("Title", "Algebra notes"), ("Type", "Document")));
        Assert.Equal(HttpStatusCode.Created, up.StatusCode);
        var material = (await Read<Material>(up))!.data!;
        Assert.False(string.IsNullOrEmpty(material.fileRecordId));
        var fileId = material.fileRecordId!;
        try
        {
            // Enrolled student (same tenant) can download the tenant-internal material.
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/v1/files/{fileId}/download")).StatusCode);

            // A student in another tenant cannot.
            var otherStudent = await TestClient.AuthedClientAsync(_factory, "STU-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await otherStudent.GetAsync($"/api/v1/files/{fileId}/download")).StatusCode);

            // A student may not upload lesson materials (role gate).
            var stuUpload = await student.PostAsync("/api/LessonMaterial/UploadMaterial",
                Form(Pdf(), "x.pdf", "application/pdf", ("LessonId", "PH8-LESSON-T1"), ("Title", "nope"), ("Type", "Document")));
            Assert.Equal(HttpStatusCode.Forbidden, stuUpload.StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"lessonMaterials\" WHERE \"FileRecordId\" = {0}", fileId);
            await DeleteFileRows(fileId);
        }
    }

    [Fact]
    public async Task ParentDocument_linked_parent_can_download_unrelated_cannot_and_audited()
    {
        // STU-T1 id (linked to PH10-PARENT-T1 in the seed fixtures).
        var stu = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var (_, stuLogin) = await TestClient.LoginAsync(TestClient.NewClient(_factory), "STU-T1");
        var studentId = stuLogin!.id!;

        var parent = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var create = await parent.PostAsJsonAsync("/api/v1/parent-requests",
            new { studentId, type = 0, title = "Report card", body = "Please share the term report." });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var requestId = (await Read<IdRow>(create))!.data!.id;
        string requestFileId = "", responseFileId = "";
        try
        {
            // Parent attaches a sensitive document to their own request.
            var attach = await parent.PostAsync($"/api/v1/parent-requests/{requestId}/attachment",
                Form(Pdf(), "id.pdf", "application/pdf"));
            Assert.Equal(HttpStatusCode.OK, attach.StatusCode);

            // Owner parent can download it; an unrelated same-tenant parent cannot.
            var dl = await parent.GetAsync($"/api/v1/parent-requests/{requestId}/attachment/download");
            Assert.Equal(HttpStatusCode.OK, dl.StatusCode);
            var unrelated = await TestClient.AuthedClientAsync(_factory, "PH11-PARENT-T1");
            Assert.Equal(HttpStatusCode.NotFound, (await unrelated.GetAsync($"/api/v1/parent-requests/{requestId}/attachment/download")).StatusCode);

            // Staff responds with a sensitive document; owner parent downloads it.
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            var respondDoc = await admin.PostAsync($"/api/v1/parent-requests/{requestId}/response-document",
                Form(Pdf(), "report.pdf", "application/pdf", ("Body", "Here is the report.")));
            Assert.Equal(HttpStatusCode.Created, respondDoc.StatusCode);
            var responseId = (await Read<IdRow>(respondDoc))!.data!.id;

            var dlResp = await parent.GetAsync($"/api/v1/parent-requests/{requestId}/responses/{responseId}/document/download");
            Assert.Equal(HttpStatusCode.OK, dlResp.StatusCode);

            // Resolve the file ids for cleanup + audit assertion.
            await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                requestFileId = (await db.parentRequests.FirstAsync(r => r.Id == requestId)).FileRecordId!;
                var sensitiveDownloads = await db.Set<DerasaX.Domain.Entities.Models.AuditLog>()
                    .Where(a => a.EntityType == "FileRecord" && a.EntityId == requestFileId)
                    .CountAsync();
                Assert.True(sensitiveDownloads >= 1, "Sensitive parent-document download must be audited.");
            }
            await using (var db2 = Phase4Db.Platform(_factory))
            {
                responseFileId = await db2.Set<DerasaX.Domain.Entities.Models.ParentRequestResponse>()
                    .Where(r => r.Id == responseId).Select(r => r.FileRecordId!).FirstAsync();
            }
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"parentRequestResponses\" WHERE \"ParentRequestId\" = {0}", requestId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"parentRequests\" WHERE \"Id\" = {0}", requestId);
            await DeleteFileRows(requestFileId, responseFileId);
        }
    }

    [Fact]
    public async Task Cv_enrollment_assets_disabled_by_default()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var status = await teacher.GetAsync("/api/v1/vision/enrollment-assets/status");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Contains("\"enabled\":false", await status.Content.ReadAsStringAsync());

        // Upload attempt is honestly refused (not faked).
        var resp = await teacher.PostAsync("/api/v1/vision/enrollments/any-id/asset",
            Form(Png(), "face.png", "image/png", ("ConsentObtained", "true")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Cv_enrollment_asset_requires_consent_and_is_tenant_scoped_when_enabled()
    {
        await using var cvFactory = new CvAssetsEnabledFactory();
        var teacher = await TestClient.AuthedClientAsync(cvFactory, "TEACH-T1");
        var (_, stuLogin) = await TestClient.LoginAsync(TestClient.NewClient(cvFactory), "STU-T1");
        var studentId = stuLogin!.id!;
        var externalLabel = "p16-" + Guid.NewGuid().ToString("N")[..10];

        var enrollResp = await teacher.PostAsJsonAsync("/api/v1/vision/enrollments",
            new { studentId, externalLabelId = externalLabel, displayLabel = "test" });
        Assert.Equal(HttpStatusCode.Created, enrollResp.StatusCode);
        var enrollmentId = (await Read<Enrollment>(enrollResp))!.data!.id;
        string assetFileId = "";
        try
        {
            // Without consent → rejected.
            var noConsent = await teacher.PostAsync($"/api/v1/vision/enrollments/{enrollmentId}/asset",
                Form(Png(), "face.png", "image/png", ("ConsentObtained", "false")));
            Assert.Equal(HttpStatusCode.BadRequest, noConsent.StatusCode);

            // With consent + retention → stored.
            var stored = await teacher.PostAsync($"/api/v1/vision/enrollments/{enrollmentId}/asset",
                Form(Png(), "face.png", "image/png", ("ConsentObtained", "true"), ("ConsentReference", "consent-2026"), ("RetentionDays", "30")));
            Assert.Equal(HttpStatusCode.OK, stored.StatusCode);

            var dl = await teacher.GetAsync($"/api/v1/vision/enrollments/{enrollmentId}/asset/download");
            Assert.Equal(HttpStatusCode.OK, dl.StatusCode);

            await using var db = Phase4Db.AsTenant(cvFactory, "tenant-1");
            var enrollment = await db.studentFaceEnrollments.FirstAsync(e => e.Id == enrollmentId);
            Assert.True(enrollment.ConsentObtained);
            Assert.NotNull(enrollment.AssetRetentionUntil);
            assetFileId = enrollment.FileRecordId!;
        }
        finally
        {
            await using var db = Phase4Db.Platform(cvFactory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", enrollmentId);
            if (!string.IsNullOrEmpty(assetFileId))
            {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", assetFileId);
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"fileRecords\" WHERE \"Id\" = {0}", assetFileId);
            }
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"studentFaceEnrollments\" WHERE \"Id\" = {0}", enrollmentId);
        }
    }
}
