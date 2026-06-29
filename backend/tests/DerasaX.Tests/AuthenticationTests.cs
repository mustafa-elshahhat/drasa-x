using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Authentication flow integration tests (Phase 3 §40).</summary>
public class AuthenticationTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AuthenticationTests(IntegrationFactory factory) => _factory = factory;

    [Fact]
    public async Task Valid_login_returns_token_and_role()
    {
        var client = TestClient.NewClient(_factory);
        var (status, body) = await TestClient.LoginAsync(client, "STU-T1");
        Assert.Equal(200, status);
        Assert.False(string.IsNullOrEmpty(body!.token));
        Assert.Equal("Student", body.role);
    }

    [Fact]
    public async Task Invalid_password_returns_401()
    {
        var client = TestClient.NewClient(_factory);
        var (status, _) = await TestClient.LoginAsync(client, "STU-T1", "wrong-password");
        Assert.Equal(401, status);
    }

    [Fact]
    public async Task Nonexistent_user_returns_same_401_as_wrong_password()
    {
        var client = TestClient.NewClient(_factory);
        var (status, _) = await TestClient.LoginAsync(client, "NO-SUCH-USER");
        Assert.Equal(401, status);
    }

    [Fact]
    public async Task Suspended_tenant_login_returns_403()
    {
        var client = TestClient.NewClient(_factory);
        var (status, _) = await TestClient.LoginAsync(client, "STU-SUS");
        Assert.Equal(403, status);
    }

    [Fact]
    public async Task Disabled_account_login_returns_401()
    {
        var client = TestClient.NewClient(_factory);
        var (status, _) = await TestClient.LoginAsync(client, "STU-DIS");
        Assert.Equal(401, status);
    }

    [Fact]
    public async Task SystemAdmin_can_login_without_tenant()
    {
        var client = TestClient.NewClient(_factory);
        var (status, body) = await TestClient.LoginAsync(client, "SYS-1");
        Assert.Equal(200, status);
        Assert.Equal("SystemAdmin", body!.role);
    }

    [Fact]
    public async Task Refresh_rotates_and_reused_old_token_is_rejected()
    {
        // Manual cookie handling so we can capture and replay a SPECIFIC token value.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false
        });

        var login = await client.PostAsJsonAsync("/api/v1/account/login", new { UserID = "TEACH-T2", Password = TestClient.Password });
        var oldCookie = ExtractRefreshCookie(login);
        Assert.False(string.IsNullOrEmpty(oldCookie));

        // Rotate using the old token -> success, returns a new token.
        var rotate = NewRequest(HttpMethod.Post, "/api/v1/account/refresh", oldCookie!);
        var rotateResp = await client.SendAsync(rotate);
        Assert.Equal(HttpStatusCode.OK, rotateResp.StatusCode);
        var newCookie = ExtractRefreshCookie(rotateResp);
        Assert.False(string.IsNullOrEmpty(newCookie));

        // Reuse the OLD (now rotated/revoked) token -> reuse detected -> 401.
        var reuse = NewRequest(HttpMethod.Post, "/api/v1/account/refresh", oldCookie!);
        var reuseResp = await client.SendAsync(reuse);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResp.StatusCode);

        // Reuse-detection revokes the whole family: the NEW token is now dead too.
        var newReq = NewRequest(HttpMethod.Post, "/api/v1/account/refresh", newCookie!);
        var newResp = await client.SendAsync(newReq);
        Assert.Equal(HttpStatusCode.Unauthorized, newResp.StatusCode);
    }

    private static HttpRequestMessage NewRequest(HttpMethod method, string url, string refreshCookie)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("Cookie", $"refreshToken={refreshCookie}");
        return req;
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

    [Fact]
    public async Task Logout_then_refresh_is_rejected()
    {
        var client = TestClient.NewClient(_factory);
        await TestClient.LoginAsync(client, "PARENT-T1");
        var logout = await client.PostAsync("/api/v1/account/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var refresh = await client.PostAsync("/api/v1/account/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Missing_refresh_cookie_returns_401()
    {
        var client = TestClient.NewClient(_factory);
        var refresh = await client.PostAsync("/api/v1/account/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
