using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// P1-6 closure — GET /api/v1/LessonMaterial/{id} lets a caller fetch a single lesson material by
/// its own id, without knowing the parent lesson id (needed by detail pages such as the student
/// material page, which only has the material id in the URL). Tenant-scoped: cross-tenant and
/// unknown ids resolve to 404, matching the pattern established by <see cref="TenantIsolationTests"/>
/// and <see cref="ResourceCommentsApiTests"/>.
/// </summary>
public class LessonMaterialDetailApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public LessonMaterialDetailApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data);
    private sealed record MaterialRow(string id, string title, string lessonId, string? fileRecordId);
    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}";

    private sealed record Mat(string subjectId, string unitId, string lessonId, string materialId);

    private async Task<Mat> SeedMaterialAsync()
    {
        var m = new Mat(NewId("sub"), NewId("unt"), NewId("les"), NewId("mat"));
        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.subjects.Add(new Subject { Id = m.subjectId, TenantId = "tenant-1", Name = "Detail-Sci", GradeId = "G7-ID" });
        db.units.Add(new Unit { Id = m.unitId, TenantId = "tenant-1", Title = "Detail-U1", SubjectId = m.subjectId });
        db.lessons.Add(new Lesson { Id = m.lessonId, TenantId = "tenant-1", Title = "Detail-L1", Content = "c", UnitId = m.unitId });
        db.lessonMaterials.Add(new LessonMaterial { Id = m.materialId, TenantId = "tenant-1", Title = "Detail-Slides", Url = "key://x", Type = AttachmentType.Slides, LessonId = m.lessonId });
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
    public async Task Tenant_member_can_get_material_by_id_and_cross_tenant_id_is_404()
    {
        var m = await SeedMaterialAsync();
        try
        {
            // A tenant-1 STUDENT (a plain tenant member, not just teacher/admin) can fetch the
            // material by its own id — the class-level TenantMember policy applies (no teacher-only
            // [Authorize] override on the new action).
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            var resp = await student.GetAsync($"/api/v1/LessonMaterial/{m.materialId}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await Read<MaterialRow>(resp);
            Assert.True(body!.success);
            Assert.Equal(m.materialId, body.data!.id);
            Assert.Equal("Detail-Slides", body.data!.title);
            Assert.Equal(m.lessonId, body.data!.lessonId);

            // A tenant-2 student requesting the tenant-1 material id -> 404 (tenant isolation).
            var t2 = await TestClient.AuthedClientAsync(_factory, "STU-T2");
            var cross = await t2.GetAsync($"/api/v1/LessonMaterial/{m.materialId}");
            Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);

            // Sanity: an unknown id (even within the caller's own tenant) is also a clean 404.
            var missing = await student.GetAsync($"/api/v1/LessonMaterial/{NewId("missing")}");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }
        finally { await Cleanup(m); }
    }

    [Fact]
    public async Task Legacy_alias_route_also_serves_material_by_id()
    {
        // The controller carries both the canonical api/v1 route and the legacy api/ alias
        // (Phase 22 Step 6 convergence) — prove the new action is wired on both.
        var m = await SeedMaterialAsync();
        try
        {
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            var resp = await student.GetAsync($"/api/LessonMaterial/{m.materialId}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
        finally { await Cleanup(m); }
    }

    [Fact]
    public async Task GetMaterialByLessonId_route_is_unaffected_by_the_new_id_route()
    {
        // Route-collision regression guard: the literal "GetMaterialByLessonId" action must keep
        // winning over the new bare "{id}" action for that exact URL (ASP.NET Core route
        // precedence: literal segments beat parameter segments).
        var m = await SeedMaterialAsync();
        try
        {
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            var resp = await student.GetAsync($"/api/v1/LessonMaterial/GetMaterialByLessonId?id={m.lessonId}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Contains(m.materialId, body);
        }
        finally { await Cleanup(m); }
    }
}
