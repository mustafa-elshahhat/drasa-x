using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 10 — Parent Portal API authorization matrix. Proves every parent read is
/// scoped to the parent's ACTIVE, progress-permitted parent-student link
/// (ParentStudentRelationship) AND tenant, with positive, no-children,
/// same-tenant-unlinked (403), cross-tenant (404), invalid-id, wrong-role, and
/// unauthenticated cases. Relies on the deterministic Phase 10 fixtures seeded by
/// DataSeederService (PH10-PARENT-T1 linked to STU-T1; PH10-PARENT-NOCHILD-T1 with
/// no links; PH8-OTHER-T1 same-tenant-unlinked; STU-T2 cross-tenant).
/// </summary>
public class ParentPortalApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public ParentPortalApiTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<JsonElement> DataAsync(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("data").Clone();
    }

    // User ids are GUIDs; the route param is the student's actual id (not the login
    // code). Resolve the id the way a client would: from the authenticated identity.
    private async Task<string> StudentIdAsync(string loginCode)
    {
        var (_, body) = await TestClient.LoginAsync(TestClient.NewClient(_factory), loginCode);
        return body!.id!;
    }

    // ---- positive: linked parent ----

    [Fact]
    public async Task Linked_parent_dashboard_counts_linked_children()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync("/api/v1/parent/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True((await DataAsync(resp)).GetProperty("linkedChildrenCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task Linked_parent_sees_only_linked_child()
    {
        var stuT1 = await StudentIdAsync("STU-T1");
        var other = await StudentIdAsync("PH8-OTHER-T1");
        var stuT2 = await StudentIdAsync("STU-T2");

        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync("/api/v1/parent/children");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = (await DataAsync(resp)).GetRawText();
        Assert.Contains(stuT1, json);
        Assert.DoesNotContain(other, json);  // same-tenant but unlinked
        Assert.DoesNotContain(stuT2, json);  // cross-tenant
    }

    [Fact]
    public async Task Linked_parent_can_open_linked_child_overview()
    {
        var stuT1 = await StudentIdAsync("STU-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync($"/api/v1/parent/children/{stuT1}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await DataAsync(resp);
        Assert.Equal(stuT1, data.GetProperty("studentId").GetString());
        // Summary is server-aggregated from the relationship-authorized read model.
        Assert.True(data.GetProperty("summary").GetProperty("lessonsTracked").GetInt32() >= 0);
    }

    [Fact]
    public async Task Linked_parent_can_read_linked_child_attendance()
    {
        var stuT1 = await StudentIdAsync("STU-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync($"/api/v1/parent/children/{stuT1}/attendance");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await DataAsync(resp);
        // STU-T1 has seeded Phase 8 attendance records; summary total is present.
        Assert.True(data.GetProperty("summary").GetProperty("total").GetInt32() >= 1);
    }

    // ---- parent with no children ----

    [Fact]
    public async Task Parent_with_no_children_dashboard_is_zero()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-NOCHILD-T1");
        var resp = await client.GetAsync("/api/v1/parent/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(0, (await DataAsync(resp)).GetProperty("linkedChildrenCount").GetInt32());
    }

    [Fact]
    public async Task Parent_with_no_children_sees_empty_children_list()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-NOCHILD-T1");
        var resp = await client.GetAsync("/api/v1/parent/children");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(0, (await DataAsync(resp)).GetArrayLength());
    }

    // ---- same-tenant but unlinked child -> 403 ----

    [Fact]
    public async Task Parent_cannot_open_same_tenant_unlinked_child_overview_403()
    {
        var other = await StudentIdAsync("PH8-OTHER-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync($"/api/v1/parent/children/{other}");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Parent_cannot_read_same_tenant_unlinked_child_attendance_403()
    {
        var other = await StudentIdAsync("PH8-OTHER-T1");
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync($"/api/v1/parent/children/{other}/attendance");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ---- cross-tenant child -> 404 (no leak) ----

    [Fact]
    public async Task Parent_cannot_open_cross_tenant_child_overview_404()
    {
        var stuT2 = await StudentIdAsync("STU-T2");
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync($"/api/v1/parent/children/{stuT2}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Parent_cannot_read_cross_tenant_child_attendance_404()
    {
        var stuT2 = await StudentIdAsync("STU-T2");
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync($"/api/v1/parent/children/{stuT2}/attendance");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---- invalid / unknown child id -> 404 ----

    [Fact]
    public async Task Parent_unknown_child_id_404()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync("/api/v1/parent/children/NOPE-DOES-NOT-EXIST");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---- wrong role / unauthenticated ----

    [Fact]
    public async Task Student_is_denied_parent_dashboard_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await client.GetAsync("/api/v1/parent/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Teacher_is_denied_parent_dashboard_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var resp = await client.GetAsync("/api/v1/parent/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SchoolAdmin_is_denied_parent_dashboard_403()
    {
        // ParentController is ParentOnly: even a same-tenant SchoolAdmin is not a parent.
        var client = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await client.GetAsync("/api/v1/parent/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_parent_read_401()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.GetAsync("/api/v1/parent/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
