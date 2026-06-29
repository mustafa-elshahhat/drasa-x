using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 closure — comments on lesson resources. Tenant members post/read; authors edit their own;
/// Teacher/SchoolAdmin moderate (delete any); cross-tenant material ids resolve to 404.
/// </summary>
public class ResourceCommentsApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public ResourceCommentsApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record IdRow(string id);
    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}";

    private sealed record Mat(string subjectId, string unitId, string lessonId, string materialId);

    private async Task<Mat> SeedMaterialAsync()
    {
        var m = new Mat(NewId("sub"), NewId("unt"), NewId("les"), NewId("mat"));
        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.subjects.Add(new Subject { Id = m.subjectId, TenantId = "tenant-1", Name = "Sci", GradeId = "G7-ID" });
        db.units.Add(new Unit { Id = m.unitId, TenantId = "tenant-1", Title = "U1", SubjectId = m.subjectId });
        db.lessons.Add(new Lesson { Id = m.lessonId, TenantId = "tenant-1", Title = "L1", Content = "c", UnitId = m.unitId });
        db.lessonMaterials.Add(new LessonMaterial { Id = m.materialId, TenantId = "tenant-1", Title = "Slides", Url = "key://x", Type = AttachmentType.Slides, LessonId = m.lessonId });
        await db.SaveChangesAsync();
        return m;
    }

    private async Task Cleanup(Mat m)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"lessonMaterialComments\" WHERE \"MaterialId\" = {0}", m.materialId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"lessonMaterials\" WHERE \"Id\" = {0}", m.materialId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"lessons\" WHERE \"Id\" = {0}", m.lessonId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"units\" WHERE \"Id\" = {0}", m.unitId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subjects\" WHERE \"Id\" = {0}", m.subjectId);
    }

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    [Fact]
    public async Task Post_list_edit_moderate_and_cross_tenant_404()
    {
        var m = await SeedMaterialAsync();
        try
        {
            var basePath = $"/api/v1/lesson-materials/{m.materialId}/comments";

            // Student posts a comment.
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            var create = await student.PostAsJsonAsync(basePath, new { body = "Great slides!" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var commentId = (await Read<IdRow>(create))!.data!.id;

            // List shows it.
            var list = await student.GetAsync($"{basePath}?pageSize=50");
            var lb = await Read<List<IdRow>>(list);
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.True(lb!.totalCount >= 1);

            // Author edits their own comment.
            Assert.Equal(HttpStatusCode.OK, (await student.PutAsJsonAsync($"{basePath}/{commentId}", new { body = "Edited" })).StatusCode);

            // A different student cannot edit it → 403.
            var otherStudent = await TestUsers.CreateLockoutStudentAsync(_factory);
            try
            {
                var other = await TestClient.AuthedClientAsync(_factory, otherStudent.LoginCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"{basePath}/{commentId}", new { body = "hijack" })).StatusCode);
            }
            finally
            {
                await using var db = Phase4Db.Platform(_factory);
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\" = {0}", otherStudent.Id);
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\" WHERE \"Id\" = {0}", otherStudent.Id);
            }

            // A teacher moderates (deletes) the comment.
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            Assert.Equal(HttpStatusCode.OK, (await teacher.DeleteAsync($"{basePath}/{commentId}")).StatusCode);

            // Cross-tenant: a tenant-2 student cannot access the tenant-1 material → 404.
            var t2 = await TestClient.AuthedClientAsync(_factory, "STU-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await t2.GetAsync(basePath)).StatusCode);
        }
        finally { await Cleanup(m); }
    }
}
