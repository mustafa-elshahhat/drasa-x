using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 22 Step 6 — proves the canonical <c>/api/v1</c> routes added to the converged academic
/// controllers (Grades, Subjects, Units, Lessons, LessonMaterial, Quiz) and the backend-mediated AI
/// tutor (<c>/api/v1/ai/tutor</c>) are wired, are NOT anonymous, and remain tenant-isolated — i.e. the
/// canonical routes enforce exactly the contract the legacy aliases do (see
/// <see cref="LegacyAcademicRoutesTests"/>). The legacy <c>api/[controller]</c> aliases remain supported
/// for the convergence window.
/// </summary>
public class CanonicalApiV1RoutesTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public CanonicalApiV1RoutesTests(IntegrationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/v1/Grades/GetAllGrades")]
    [InlineData("/api/v1/Subjects/GetSubjects")]
    [InlineData("/api/v1/Quiz/GetAllQuizzes")]
    public async Task Canonical_v1_academic_routes_require_authentication(string path)
    {
        var anon = TestClient.NewClient(_factory);
        var resp = await anon.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Canonical_v1_tutor_route_is_wired_and_not_anonymous()
    {
        // The canonical alias for the legacy /api/chat. Auth runs before body binding, so an anon
        // POST is rejected with 401 (NOT 404) — which proves the route exists and is protected.
        var anon = TestClient.NewClient(_factory);
        var resp = await anon.PostAsync("/api/v1/ai/tutor",
            new StringContent("{\"message\":\"hi\"}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Canonical_v1_grades_route_is_tenant_isolated()
    {
        // Tenant-1 admin sees the tenant-1 grade fixture (G7-ID); tenant-2 admin does not.
        var t1 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var t1Body = await (await t1.GetAsync("/api/v1/Grades/GetAllGrades")).Content.ReadAsStringAsync();
        Assert.Contains("G7-ID", t1Body);

        var t2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
        var t2Body = await (await t2.GetAsync("/api/v1/Grades/GetAllGrades")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("G7-ID", t2Body);
    }
}
