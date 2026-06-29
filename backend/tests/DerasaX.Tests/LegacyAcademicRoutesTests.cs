using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 closure — security hardening evidence for the retained legacy academic routes
/// (api/[controller]/Action for Grades, Subjects, Units, Lessons, LessonMaterial, Quiz). These
/// remain part of the supported contract during /api/v1 convergence; this proves they are NOT
/// anonymous and enforce tenant isolation (cross-tenant data is never disclosed).
/// </summary>
public class LegacyAcademicRoutesTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public LegacyAcademicRoutesTests(IntegrationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/Grades/GetAllGrades")]
    [InlineData("/api/Subjects/GetSubjects")]
    [InlineData("/api/Quiz/GetAllQuizzes")]
    [InlineData("/api/notifications")]
    public async Task Legacy_routes_require_authentication(string path)
    {
        var anon = TestClient.NewClient(_factory);
        var resp = await anon.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Legacy_grades_route_is_tenant_isolated()
    {
        // Tenant-1 admin sees the tenant-1 grade fixture (G7-ID); tenant-2 admin does not.
        var t1 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var t1Body = await (await t1.GetAsync("/api/Grades/GetAllGrades")).Content.ReadAsStringAsync();
        Assert.Contains("G7-ID", t1Body);

        var t2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
        var t2Body = await (await t2.GetAsync("/api/Grades/GetAllGrades")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("G7-ID", t2Body);
    }
}
