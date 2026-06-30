using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 11 — School Admin Portal API authorization matrix for the NEW admin contracts
/// (aggregate dashboard, parent↔student relationship management, teacher↔class assignment
/// management). Proves: real tenant-scoped dashboard, tenant isolation, create/duplicate/
/// validation paths, cross-tenant & wrong-role-target 404 (no leak), and the wrong-role (403) /
/// unauthenticated (401) gate. Relies on the deterministic Phase 11 fixtures seeded by
/// DataSeederService (PH11-SCHOOLADMIN-T1/T2, PH11-PARENT-T1, PH11-STUDENT-T1, PH11-TEACHER-T1,
/// PH11-CLASS-T1) plus the existing tenant-1 data (PH10-PSR-T1 link, PH9-TCA-T1 assignment).
/// Mutations are cleaned up directly (Phase4Db) so the suite is repeatable across runs.
/// </summary>
public class SchoolAdminPortalApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public SchoolAdminPortalApiTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<JsonElement> DataAsync(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("data").Clone();
    }

    // Resolve a user's GUID id the way a client does — from the authenticated identity.
    private async Task<string> IdOf(string loginCode)
    {
        var (_, body) = await TestClient.LoginAsync(TestClient.NewClient(_factory), loginCode);
        return body!.id!;
    }

    private async Task DeleteRelationshipsByParent(string parentId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.parentStudentRelationships.IgnoreQueryFilters().Where(r => r.ParentId == parentId).ExecuteDeleteAsync();
    }

    private async Task DeleteAssignmentsByTeacher(string teacherId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.teacherClassAssignments.IgnoreQueryFilters().Where(a => a.TeacherId == teacherId).ExecuteDeleteAsync();
    }

    // ---- dashboard: real tenant summary + isolation ----

    [Fact]
    public async Task Admin_dashboard_returns_real_tenant_summary()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        var resp = await client.GetAsync("/api/v1/school-admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var d = await DataAsync(resp);
        Assert.Equal("Nile Future International School", d.GetProperty("tenantName").GetString());
        // Real counts from authoritative data — tenant-1 has seeded students/teachers/classes.
        Assert.True(d.GetProperty("students").GetInt32() >= 1);
        Assert.True(d.GetProperty("teachers").GetInt32() >= 1);
        Assert.True(d.GetProperty("classes").GetInt32() >= 1);
        Assert.True(d.GetProperty("parentStudentLinks").GetInt32() >= 1); // PH10-PSR-T1
    }

    [Fact]
    public async Task Dashboard_is_tenant_scoped()
    {
        var t1 = await DataAsync(await (await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1")).GetAsync("/api/v1/school-admin/dashboard"));
        var t2 = await DataAsync(await (await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T2")).GetAsync("/api/v1/school-admin/dashboard"));
        Assert.Equal("Nile Future International School", t1.GetProperty("tenantName").GetString());
        Assert.Equal("Al-Nahda STEM School", t2.GetProperty("tenantName").GetString());
        // tenant-2 is a separate, smaller tenant — it must not inherit tenant-1's totals.
        Assert.True(t1.GetProperty("students").GetInt32() > t2.GetProperty("students").GetInt32());
    }

    // ---- relationships: list, isolation, create/duplicate, negative cases ----

    [Fact]
    public async Task Admin_lists_relationships_includes_seeded_link()
    {
        var stuT1 = await IdOf("STU-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        var resp = await client.GetAsync("/api/v1/school-admin/relationships");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await DataAsync(resp);
        Assert.True(data.GetArrayLength() >= 1);
        Assert.Contains(stuT1, data.GetRawText()); // PH10-PSR-T1 links a parent to STU-T1
    }

    [Fact]
    public async Task Relationships_are_tenant_scoped()
    {
        var stuT1 = await IdOf("STU-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T2");
        var resp = await client.GetAsync("/api/v1/school-admin/relationships");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.DoesNotContain(stuT1, (await DataAsync(resp)).GetRawText()); // tenant-1 child must not leak to tenant-2 admin
    }

    [Fact]
    public async Task Admin_creates_link_then_rejects_duplicate()
    {
        var parentId = await IdOf("PH11-PARENT-T1");
        var studentId = await IdOf("PH11-STUDENT-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        try
        {
            await DeleteRelationshipsByParent(parentId); // ensure a clean starting point (repeatable)

            var create = await client.PostAsJsonAsync("/api/v1/school-admin/relationships",
                new { parentId, studentId, relationship = 2, isPrimary = true, canViewProgress = true });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var d = await DataAsync(create);
            Assert.Equal(parentId, d.GetProperty("parentId").GetString());
            Assert.Equal(studentId, d.GetProperty("studentId").GetString());
            Assert.True(d.GetProperty("isActive").GetBoolean());

            // Duplicate active link → 409.
            var dup = await client.PostAsJsonAsync("/api/v1/school-admin/relationships", new { parentId, studentId });
            Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        }
        finally { await DeleteRelationshipsByParent(parentId); }
    }

    [Fact]
    public async Task Create_relationship_cross_tenant_parent_404()
    {
        var crossParent = await IdOf("PARENT-T2"); // tenant-2 parent
        var studentId = await IdOf("PH11-STUDENT-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        var resp = await client.PostAsJsonAsync("/api/v1/school-admin/relationships", new { parentId = crossParent, studentId });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode); // no existence leak
    }

    [Fact]
    public async Task Create_relationship_wrong_role_target_404()
    {
        // A Student id supplied where a Parent is required → 404 (not a Parent in this tenant).
        var notAParent = await IdOf("STU-T1");
        var studentId = await IdOf("PH11-STUDENT-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        var resp = await client.PostAsJsonAsync("/api/v1/school-admin/relationships", new { parentId = notAParent, studentId });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Create_relationship_missing_fields_400()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        var resp = await client.PostAsJsonAsync("/api/v1/school-admin/relationships", new { parentId = "", studentId = "" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ---- teacher-class assignments: list, create/duplicate, negative cases ----

    [Fact]
    public async Task Admin_lists_class_assignments_includes_seeded()
    {
        var teachT1 = await IdOf("TEACH-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        var resp = await client.GetAsync("/api/v1/school-admin/teacher-class-assignments");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await DataAsync(resp);
        Assert.True(data.GetArrayLength() >= 1); // PH9-TCA-T1
        Assert.Contains(teachT1, data.GetRawText());
    }

    [Fact]
    public async Task Admin_creates_class_assignment_then_rejects_duplicate()
    {
        var teacherId = await IdOf("PH11-TEACHER-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        try
        {
            await DeleteAssignmentsByTeacher(teacherId);

            var create = await client.PostAsJsonAsync("/api/v1/school-admin/teacher-class-assignments",
                new { teacherId, schoolClassId = "PH11-CLASS-T1", role = 0 });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var d = await DataAsync(create);
            Assert.Equal(teacherId, d.GetProperty("teacherId").GetString());
            Assert.Equal("PH11-CLASS-T1", d.GetProperty("schoolClassId").GetString());

            var dup = await client.PostAsJsonAsync("/api/v1/school-admin/teacher-class-assignments",
                new { teacherId, schoolClassId = "PH11-CLASS-T1" });
            Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        }
        finally { await DeleteAssignmentsByTeacher(teacherId); }
    }

    [Fact]
    public async Task Create_class_assignment_cross_tenant_class_404()
    {
        var teacherId = await IdOf("PH11-TEACHER-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        // PH8-CLASS-T2 belongs to tenant-2 → invisible to a tenant-1 admin → 404.
        var resp = await client.PostAsJsonAsync("/api/v1/school-admin/teacher-class-assignments",
            new { teacherId, schoolClassId = "PH8-CLASS-T2" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Create_class_assignment_wrong_role_teacher_404()
    {
        var notATeacher = await IdOf("STU-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH11-SCHOOLADMIN-T1");
        var resp = await client.PostAsJsonAsync("/api/v1/school-admin/teacher-class-assignments",
            new { teacherId = notATeacher, schoolClassId = "PH11-CLASS-T1" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---- wrong role (403) / unauthenticated (401) on the school-admin surface ----

    [Theory]
    [InlineData("STU-T1")]
    [InlineData("TEACH-T1")]
    [InlineData("PARENT-T1")]
    [InlineData("SYS-1")] // SystemAdmin is not a SchoolAdmin
    public async Task Non_school_admin_is_denied_403(string loginCode)
    {
        var client = await TestClient.AuthedClientAsync(_factory, loginCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/school-admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/school-admin/relationships")).StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_is_denied_401()
    {
        var client = TestClient.NewClient(_factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/school-admin/dashboard")).StatusCode);
    }
}
