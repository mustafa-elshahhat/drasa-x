using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 closure — SchoolAdmin provisioning of tenant Student/Teacher accounts and credentials.
/// Proves: create returns a one-time credential the new account can actually log in with; duplicate
/// login codes 409; admin roles cannot be provisioned (403); disable blocks login; reset rotates the
/// credential; cross-tenant ids 404; non-admin callers 403; the temp password never appears in logs/audit.
/// </summary>
public class UserProvisioningApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public UserProvisioningApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data);
    private sealed record Cred(string userId, string loginCode, string role, string temporaryPassword);
    private static string NewCode(string p) => $"{p}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    private async Task Delete(params string[] userIds)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var id in userIds)
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", id);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\" = {0}", id);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\" WHERE \"Id\" = {0}", id);
        }
    }

    [Fact]
    public async Task SchoolAdmin_provisions_student_who_can_login_with_returned_credential()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var code = NewCode("PROV-STU");
        string? newId = null;
        try
        {
            var create = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = "Provisioned Student", loginCode = code, role = "Student", gradeId = "G7-ID" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var cred = (await Read<Cred>(create))!.data!;
            newId = cred.userId;
            Assert.Equal("Student", cred.role);
            Assert.False(string.IsNullOrWhiteSpace(cred.temporaryPassword));

            // The returned one-time credential actually authenticates.
            var (status, body) = await TestClient.LoginAsync(TestClient.NewClient(_factory), code, cred.temporaryPassword);
            Assert.Equal((int)HttpStatusCode.OK, status);
            Assert.False(string.IsNullOrEmpty(body!.token));

            // Audit row exists but must NOT contain the temporary password.
            await using var db = Phase4Db.Platform(_factory);
            var audit = await db.auditLogs.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.EntityType == "ApplicationUser" && a.EntityId == newId);
            Assert.NotNull(audit);
            Assert.DoesNotContain(cred.temporaryPassword, audit!.MetadataJson ?? "");

            // Duplicate login code → 409.
            var dup = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = "Dup", loginCode = code, role = "Teacher" });
            Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        }
        finally { if (newId != null) await Delete(newId); }
    }

    [Fact]
    public async Task Admin_roles_cannot_be_provisioned_and_non_admins_are_forbidden()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        // SchoolAdmin/SystemAdmin provisioning is refused (no privilege escalation).
        var esc = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = "x", loginCode = NewCode("ESC"), role = "SchoolAdmin" });
        Assert.Equal(HttpStatusCode.Forbidden, esc.StatusCode);

        // A teacher cannot use the provisioning surface at all → 403.
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync("/api/v1/tenant-users")).StatusCode);
    }

    [Fact]
    public async Task Disable_blocks_login_and_reset_rotates_credential_with_tenant_isolation()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var code = NewCode("PROV-TCH");
        string? newId = null;
        try
        {
            var create = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = "Provisioned Teacher", loginCode = code, role = "Teacher" });
            var cred = (await Read<Cred>(create))!.data!;
            newId = cred.userId;

            // Disable → login is refused.
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/tenant-users/{newId}/disable", null)).StatusCode);
            var (disabledStatus, _) = await TestClient.LoginAsync(TestClient.NewClient(_factory), code, cred.temporaryPassword);
            Assert.NotEqual((int)HttpStatusCode.OK, disabledStatus);

            // Re-enable + reset credential → the NEW credential logs in.
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/tenant-users/{newId}/enable", null)).StatusCode);
            var reset = await admin.PostAsync($"/api/v1/tenant-users/{newId}/reset-credential", null);
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            var rotated = (await Read<Cred>(reset))!.data!;
            Assert.NotEqual(cred.temporaryPassword, rotated.temporaryPassword);
            var (rotatedStatus, _) = await TestClient.LoginAsync(TestClient.NewClient(_factory), code, rotated.temporaryPassword);
            Assert.Equal((int)HttpStatusCode.OK, rotatedStatus);

            // A tenant-2 admin cannot see/modify this tenant-1 account → 404 (no cross-tenant leak).
            var otherAdmin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await otherAdmin.GetAsync($"/api/v1/tenant-users/{newId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await otherAdmin.PostAsync($"/api/v1/tenant-users/{newId}/disable", null)).StatusCode);
        }
        finally { if (newId != null) await Delete(newId); }
    }
}
