using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Forced first-login password change: newly-provisioned/reset accounts get
/// <c>MustChangePassword = true</c>; the gate middleware blocks every endpoint but a small
/// allowlist until the password is changed; changing it (a) rejects a no-op same-password
/// "change", (b) clears the flag, and (c) issues a fresh refresh token so a subsequent
/// <c>/refresh</c> call returns an access token whose claim reflects the cleared flag.
/// </summary>
public class MustChangePasswordApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public MustChangePasswordApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data);
    private sealed record Cred(string userId, string loginCode, string role, string temporaryPassword);

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    private async Task Delete(string userId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", userId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\" = {0}", userId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\" WHERE \"Id\" = {0}", userId);
    }

    [Fact]
    public async Task Newly_provisioned_account_has_must_change_password_flag_set_in_the_database()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        string? newId = null;
        try
        {
            var create = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = "Flagged Teacher", role = "Teacher" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var cred = (await Read<Cred>(create))!.data!;
            newId = cred.userId;

            await using var db = Phase4Db.Platform(_factory);
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == newId);
            Assert.NotNull(user);
            Assert.True(user!.MustChangePassword);
        }
        finally { if (newId != null) await Delete(newId); }
    }

    [Fact]
    public async Task Temp_password_login_is_blocked_from_other_endpoints_until_password_is_changed_then_unblocked()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        string? newId = null;
        try
        {
            var create = await admin.PostAsJsonAsync("/api/v1/tenant-users", new { fullName = "Gated Teacher", role = "Teacher" });
            var cred = (await Read<Cred>(create))!.data!;
            newId = cred.userId;

            var client = TestClient.NewClient(_factory); // HandleCookies = true — carries the rotated refresh cookie automatically.
            var (loginStatus, loginBody) = await TestClient.LoginAsync(client, cred.loginCode, cred.temporaryPassword);
            Assert.Equal(200, loginStatus);
            Assert.True(loginBody!.mustChangePassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.token);

            // Blocked: a non-allowlisted, otherwise-authorized endpoint (Teacher may normally list nothing here,
            // but SelfAccount-only account routes are what matter — use a Teacher-authorized tenant route).
            var blocked = await client.GetAsync("/api/v1/notifications");
            Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
            Assert.Contains("PASSWORD_CHANGE_REQUIRED", await blocked.Content.ReadAsStringAsync());

            // Allowlisted even while flagged: revoke-own-session (SelfAccount policy).
            var revoke = await client.PostAsJsonAsync("/api/v1/account/revoke", new { });
            Assert.NotEqual(HttpStatusCode.Forbidden, revoke.StatusCode);

            // Rejected: "changing" to the exact same password.
            var sameChange = await client.PostAsJsonAsync("/api/v1/account/change-password",
                new { CurrentPassword = cred.temporaryPassword, NewPassword = cred.temporaryPassword });
            Assert.Equal(HttpStatusCode.BadRequest, sameChange.StatusCode);

            // Real change succeeds.
            const string newPassword = "Fresh#Pass9000";
            var change = await client.PostAsJsonAsync("/api/v1/account/change-password",
                new { CurrentPassword = cred.temporaryPassword, NewPassword = newPassword });
            Assert.Equal(HttpStatusCode.OK, change.StatusCode);

            // The DB flag is cleared immediately...
            await using (var db = Phase4Db.Platform(_factory))
            {
                var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == newId);
                Assert.False(user!.MustChangePassword);
            }

            // ...and refreshing (using the cookie rotated by the change-password response) returns an
            // access token whose claim reflects the cleared flag, unblocking the previously-403 route.
            var refresh = await client.PostAsync("/api/v1/account/refresh", null);
            Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
            var refreshed = await refresh.Content.ReadFromJsonAsync<TestClient.LoginResponse>(Json);
            Assert.False(refreshed!.mustChangePassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.token);

            var unblocked = await client.GetAsync("/api/v1/notifications");
            Assert.NotEqual(HttpStatusCode.Forbidden, unblocked.StatusCode);
        }
        finally { if (newId != null) await Delete(newId); }
    }
}
