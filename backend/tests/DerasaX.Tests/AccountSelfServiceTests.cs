using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 3 closure §3.1 — self-account operations must work for ANY authenticated
/// user, including a platform SystemAdmin that has no tenant claim, while still
/// being denied access to ordinary tenant-domain routes.
/// </summary>
public class AccountSelfServiceTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AccountSelfServiceTests(IntegrationFactory factory) => _factory = factory;

    private HttpClient ManualCookieClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task SystemAdmin_can_change_own_password_and_old_password_stops_working()
    {
        var u = await TestUsers.CreateSystemAdminAsync(_factory, "Local@Dev123");
        var newPassword = "Local@Dev999";

        // Login with the original password and capture both access token + refresh cookie.
        var client = ManualCookieClient();
        var login = await client.PostAsJsonAsync("/api/v1/account/login",
            new { UserID = u.LoginCode, Password = u.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await ExtractToken(login);
        var oldRefresh = ExtractRefreshCookie(login);
        Assert.False(string.IsNullOrEmpty(token));
        Assert.False(string.IsNullOrEmpty(oldRefresh));

        // Change password as the SystemAdmin (SelfAccount policy, no tenant claim).
        var change = new HttpRequestMessage(HttpMethod.Post, "/api/v1/account/change-password")
        {
            Content = JsonContent.Create(new { CurrentPassword = u.Password, NewPassword = newPassword })
        };
        change.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var changeResp = await client.SendAsync(change);
        Assert.Equal(HttpStatusCode.OK, changeResp.StatusCode);

        // Old password no longer works.
        var oldLogin = await client.PostAsJsonAsync("/api/v1/account/login",
            new { UserID = u.LoginCode, Password = u.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        // New password works.
        var newLogin = await client.PostAsJsonAsync("/api/v1/account/login",
            new { UserID = u.LoginCode, Password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        // The pre-change refresh session was revoked (password-change revokes all sessions).
        var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/v1/account/refresh");
        refresh.Headers.Add("Cookie", $"refreshToken={oldRefresh}");
        var refreshResp = await client.SendAsync(refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_can_revoke_own_refresh_session()
    {
        var u = await TestUsers.CreateSystemAdminAsync(_factory, "Local@Dev123");

        var client = ManualCookieClient();
        var login = await client.PostAsJsonAsync("/api/v1/account/login",
            new { UserID = u.LoginCode, Password = u.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await ExtractToken(login);
        var refreshCookie = ExtractRefreshCookie(login);
        Assert.False(string.IsNullOrEmpty(refreshCookie));

        // Revoke own session via the cookie-bound token (SelfAccount, no tenant).
        var revoke = new HttpRequestMessage(HttpMethod.Post, "/api/v1/account/revoke")
        {
            Content = JsonContent.Create(new { Token = (string?)null })
        };
        revoke.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        revoke.Headers.Add("Cookie", $"refreshToken={refreshCookie}");
        var revokeResp = await client.SendAsync(revoke);
        Assert.Equal(HttpStatusCode.OK, revokeResp.StatusCode);

        // The revoked session can no longer refresh.
        var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/v1/account/refresh");
        refresh.Headers.Add("Cookie", $"refreshToken={refreshCookie}");
        var refreshResp = await client.SendAsync(refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_is_still_forbidden_on_tenant_domain_routes()
    {
        var u = await TestUsers.CreateSystemAdminAsync(_factory, "Local@Dev123");
        var client = await TestClient.AuthedClientAsync(_factory, u.LoginCode);
        var resp = await client.GetAsync("/api/Grades/GetAllGrades");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    private static async Task<string?> ExtractToken(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadFromJsonAsync<TestClient.LoginResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return body?.token;
    }

    private static string? ExtractRefreshCookie(HttpResponseMessage resp)
    {
        if (!resp.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
        foreach (var c in cookies)
        {
            if (c.StartsWith("refreshToken="))
            {
                var v = c.Substring("refreshToken=".Length);
                var semi = v.IndexOf(';');
                return semi >= 0 ? v.Substring(0, semi) : v;
            }
        }
        return null;
    }
}
