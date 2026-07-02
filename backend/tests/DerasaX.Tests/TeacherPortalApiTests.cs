using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 9 — Teacher Portal API authorization matrix. Proves every teacher read is
/// scoped to the teacher's ACTIVE assignment (TeacherClassAssignment /
/// TeacherSubjectAssignment) AND tenant, with positive, unassigned-negative,
/// cross-tenant, and wrong-role cases. Relies on the deterministic Phase 9
/// fixtures seeded by DataSeederService (TEACH-T1 assigned to PH8-CLASS-T1 /
/// PH8-SUBJECT-T1; TEACH-T9-UNASSIGNED with no assignment; TEACH-T2 in tenant-2).
/// </summary>
public class TeacherPortalApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public TeacherPortalApiTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<JsonElement> DataAsync(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("data").Clone();
    }

    // ---- positive: assigned teacher ----

    [Fact]
    public async Task Assigned_teacher_dashboard_is_scoped_and_nonzero()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var resp = await client.GetAsync("/api/v1/teacher/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await DataAsync(resp);
        Assert.True(data.GetProperty("assignedClassCount").GetInt32() >= 1);
        Assert.True(data.GetProperty("assignedSubjectCount").GetInt32() >= 1);
        Assert.True(data.GetProperty("studentCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task Assigned_teacher_sees_only_assigned_class()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var resp = await client.GetAsync("/api/v1/teacher/classes");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await DataAsync(resp);
        var json = data.GetRawText();
        Assert.Contains("PH8-CLASS-T1", json);
        Assert.DoesNotContain("PH8-CLASS-T2", json); // cross-tenant class never present
    }

    [Fact]
    public async Task Assigned_teacher_sees_only_assigned_subject()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var resp = await client.GetAsync("/api/v1/teacher/subjects");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("PH8-SUBJECT-T1", (await DataAsync(resp)).GetRawText());
    }

    [Fact]
    public async Task Assigned_teacher_sees_enrolled_students_of_assigned_class()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var resp = await client.GetAsync("/api/v1/teacher/classes/PH8-CLASS-T1/students");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        // STU-T1 is actively enrolled in PH8-CLASS-T1.
        Assert.True((await DataAsync(resp)).GetArrayLength() >= 1);
    }

    // ---- negative: unassigned same-tenant teacher ----

    [Fact]
    public async Task Unassigned_teacher_dashboard_has_zero_scope()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T9-UNASSIGNED");
        var resp = await client.GetAsync("/api/v1/teacher/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await DataAsync(resp);
        Assert.Equal(0, data.GetProperty("assignedClassCount").GetInt32());
        Assert.Equal(0, data.GetProperty("studentCount").GetInt32());
    }

    [Fact]
    public async Task Unassigned_teacher_class_list_is_empty()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T9-UNASSIGNED");
        var resp = await client.GetAsync("/api/v1/teacher/classes");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(0, (await DataAsync(resp)).GetArrayLength());
    }

    [Fact]
    public async Task Unassigned_teacher_cannot_read_students_of_unassigned_class_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T9-UNASSIGNED");
        var resp = await client.GetAsync("/api/v1/teacher/classes/PH8-CLASS-T1/students");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ---- cross-tenant isolation ----

    [Fact]
    public async Task Teacher_cannot_read_students_of_cross_tenant_class_404()
    {
        // TEACH-T2 (tenant-2) asks for a tenant-1 class -> filtered to "not found".
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T2");
        var resp = await client.GetAsync("/api/v1/teacher/classes/PH8-CLASS-T1/students");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Cross_tenant_teacher_class_list_excludes_other_tenant()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T2");
        var resp = await client.GetAsync("/api/v1/teacher/classes");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = (await DataAsync(resp)).GetRawText();
        Assert.DoesNotContain("PH8-CLASS-T1", json);
    }

    // ---- wrong role / unauthenticated ----

    [Fact]
    public async Task Student_is_denied_teacher_dashboard_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await client.GetAsync("/api/v1/teacher/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Parent_is_denied_teacher_dashboard_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");
        var resp = await client.GetAsync("/api/v1/teacher/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_teacher_read_401()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.GetAsync("/api/v1/teacher/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ---- SchoolAdmin: Teacher-portal removal (SchoolAdmin must not reach Teacher-personal endpoints) ----

    // SchoolAdmin Teacher-portal removal: TeacherController used to grant SchoolAdmin a
    // tenant-wide bypass (this test previously asserted 200 OK here). That is no longer the
    // desired product behavior — /api/v1/teacher/* is the Teacher's own personal dashboard/
    // assignment surface, not a school-administration endpoint, so SchoolAdmin now gets 403
    // like any other non-Teacher role. SchoolAdmin's equivalent administrative views live
    // under /api/v1/school-admin/* instead.
    [Fact]
    public async Task SchoolAdmin_dashboard_is_forbidden_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await client.GetAsync("/api/v1/teacher/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SchoolAdmin_classes_is_forbidden_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await client.GetAsync("/api/v1/teacher/classes");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SchoolAdmin_class_students_is_forbidden_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await client.GetAsync("/api/v1/teacher/classes/PH8-CLASS-T1/students");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
