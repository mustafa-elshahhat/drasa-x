using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace DerasaX.Tests;

/// <summary>Shared helpers for the integration tests (login, token extraction).</summary>
public static class TestClient
{
    public const string Password = "Local@Dev123";

    public record LoginResponse(string? token, string? role, string? id, bool isAuthenticated, bool mustChangePassword = false);

    public static HttpClient NewClient(IntegrationFactory factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

    public static async Task<(int status, LoginResponse? body)> LoginAsync(HttpClient client, string loginCode, string? password = null)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/account/login", new { UserID = loginCode, Password = password ?? Password });
        LoginResponse? body = null;
        if (resp.IsSuccessStatusCode)
            body = await resp.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return ((int)resp.StatusCode, body);
    }

    /// <summary>
    /// Login and return a client whose Authorization header carries the access token. The whole
    /// integration suite runs in-process with xUnit class-parallelism, so many tests hammer the
    /// shared seed logins at once; a brief bounded retry absorbs transient login contention (e.g.
    /// connection-pool pressure) without masking a real failure — a genuinely bad credential keeps
    /// returning null and still surfaces as a null-reference at the call site.
    /// </summary>
    public static async Task<HttpClient> AuthedClientAsync(IntegrationFactory factory, string loginCode)
    {
        var client = NewClient(factory);
        LoginResponse? body = null;
        for (var attempt = 0; attempt < 4 && string.IsNullOrEmpty(body?.token); attempt++)
        {
            if (attempt > 0) await Task.Delay(150 * attempt);
            (_, body) = await LoginAsync(client, loginCode);
        }
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body!.token);
        return client;
    }
}
