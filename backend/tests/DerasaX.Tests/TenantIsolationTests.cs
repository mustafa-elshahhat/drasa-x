using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Cross-tenant isolation and object-ID tampering tests using two real tenants
/// (Tenant 1 and Tenant 2) with overlapping roles and distinct records
/// (Phase 3 §42, TENANT_ISOLATION §9).
/// </summary>
public class TenantIsolationTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public TenantIsolationTests(IntegrationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetById_of_other_tenant_object_returns_404()
    {
        // Tenant-1 student requests Tenant-2 grade id (T2-G7) -> must be 404, not data.
        var client = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await client.GetAsync("/api/Grades/GetGradeById?id=T2-G7");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_of_own_tenant_object_returns_200()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await client.GetAsync("/api/Grades/GetGradeById?id=G7-ID");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task List_is_scoped_to_caller_tenant_only()
    {
        var t1 = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var t2 = await TestClient.AuthedClientAsync(_factory, "STU-T2");

        var t1Body = await (await t1.GetAsync("/api/Grades/GetAllGrades")).Content.ReadAsStringAsync();
        var t2Body = await (await t2.GetAsync("/api/Grades/GetAllGrades")).Content.ReadAsStringAsync();

        // Tenant 1's seeded grade (G7-ID) must not appear for Tenant 2, and vice versa.
        Assert.Contains("G7-ID", t1Body);
        Assert.DoesNotContain("T2-G7", t1Body);

        Assert.Contains("T2-G7", t2Body);
        Assert.DoesNotContain("G7-ID", t2Body);
    }

    [Fact]
    public async Task Cross_tenant_delete_by_id_does_not_affect_other_tenant()
    {
        // Tenant-1 teacher attempts to delete Tenant-2's grade id -> no effect (filtered).
        var t1Teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        await t1Teacher.DeleteAsync("/api/Grades/DeleteGrade?id=T2-G7");

        // Tenant-2 still sees its grade.
        var t2 = await TestClient.AuthedClientAsync(_factory, "STU-T2");
        var body = await (await t2.GetAsync("/api/Grades/GetGradeById?id=T2-G7")).Content.ReadAsStringAsync();
        Assert.Contains("T2-G7", body);
    }
}
