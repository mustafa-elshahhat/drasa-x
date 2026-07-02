using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Testing gap T-08 (route/detail/RBAC audit §10 item 8): pins the full
/// "tenant 403-vs-404" contract on the SAME concrete routes in one place —
/// previously the two halves were proven separately, on different endpoints,
/// in different test classes (<see cref="TenantIsolationTests"/> proves the
/// 404 half on GetGradeById; <see cref="AuthorizationMatrixTests"/> proves the
/// 403 half on GetAllGrades) — leaving it implicit, not verified, that both
/// halves hold for the SAME resource.
///
/// The contract: a caller who legitimately belongs to SOME tenant but
/// requests another tenant's object gets 404 (existence-hiding — they have a
/// real session, just not for this record, so nothing should be revealed by
/// the response code). A caller with NO tenant membership at all
/// (SystemAdmin) requesting ANY tenant-scoped route gets 403 (there is
/// nothing to hide — they are correctly told they lack access to this
/// resource class entirely, not left to infer whether any particular record
/// exists).
/// </summary>
public class TenantRouteContractTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public TenantRouteContractTests(IntegrationFactory factory) => _factory = factory;

    [Fact]
    public async Task Cross_tenant_same_role_object_read_is_404_not_403()
    {
        var t1Student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await t1Student.GetAsync("/api/v1/Grades/GetGradeById?id=T2-G7");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_same_route_is_403_not_404()
    {
        // SystemAdmin has no tenant claim at all, so — unlike the cross-tenant
        // case above — there is no "wrong tenant" object to hide; they are
        // rejected outright by the TenantMember policy before any lookup runs.
        var sysAdmin = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var resp = await sysAdmin.GetAsync("/api/v1/Grades/GetGradeById?id=T2-G7");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_list_route_is_403_not_a_silent_empty_200()
    {
        // A silently-empty 200 would look identical to "this tenant just has no
        // grades yet" from the caller's perspective — 403 makes the rejection
        // explicit instead of indistinguishable from a legitimate empty tenant.
        var sysAdmin = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var resp = await sysAdmin.GetAsync("/api/v1/Grades/GetAllGrades");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Own_tenant_object_read_is_200_completing_the_contract()
    {
        var t1Student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await t1Student.GetAsync("/api/v1/Grades/GetGradeById?id=G7-ID");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
