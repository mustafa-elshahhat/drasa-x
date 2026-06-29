using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Role authorization-matrix tests across real routes (Phase 3 §41).</summary>
public class AuthorizationMatrixTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AuthorizationMatrixTests(IntegrationFactory factory) => _factory = factory;

    [Fact]
    public async Task Unauthenticated_read_returns_401()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.GetAsync("/api/Grades/GetAllGrades");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task TenantMember_can_read()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await client.GetAsync("/api/Grades/GetAllGrades");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Student_cannot_write_grade_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await client.PostAsJsonAsync("/api/Grades/AddGrade", new { name = "X" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Teacher_can_write_grade()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var resp = await client.PostAsJsonAsync("/api/Grades/AddGrade", new { name = "TeacherGrade" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task SchoolAdmin_can_write_grade()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var resp = await client.PostAsJsonAsync("/api/Grades/AddGrade", new { name = "AdminGrade" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Platform_SystemAdmin_is_not_a_tenant_member_on_tenant_routes_403()
    {
        // SystemAdmin (no tenant claim) must use explicit platform endpoints, not
        // generic tenant-member reads -> 403 on a TenantMember-policy route.
        var client = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var resp = await client.GetAsync("/api/Grades/GetAllGrades");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
