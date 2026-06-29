using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 3 closure §3.2 — deterministic account-lockout lifecycle:
///   failed attempts -> threshold reached -> account locked ->
///   correct password rejected while locked (canonical 401) ->
///   recovery after the lockout window (simulated by clearing lockout state,
///   NOT by sleeping for the real 15-minute window).
/// </summary>
public class LockoutLifecycleTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public LockoutLifecycleTests(IntegrationFactory factory) => _factory = factory;

    // Mirrors Auth:Lockout:MaxFailedAttempts (default 5).
    private const int Threshold = 5;

    [Fact]
    public async Task Lockout_threshold_locks_account_and_recovers_after_window()
    {
        var u = await TestUsers.CreateLockoutStudentAsync(_factory, "Local@Dev123");
        var client = TestClient.NewClient(_factory);

        // Drive failed attempts up to (but not past) the threshold. Each wrong
        // password increments AccessFailedCount and returns a canonical 401.
        for (var i = 0; i < Threshold; i++)
        {
            var (status, _) = await TestClient.LoginAsync(client, u.LoginCode, "definitely-wrong");
            Assert.Equal(401, status);
        }

        // The account is now locked: even the CORRECT password is rejected, and the
        // response is the same non-enumerating 401 (no account-state disclosure).
        var (lockedStatus, lockedBody) = await TestClient.LoginAsync(client, u.LoginCode, u.Password);
        Assert.Equal(401, lockedStatus);
        Assert.Null(lockedBody); // no token issued

        // Still locked on a repeat correct-password attempt (lockout is durable).
        var (stillLocked, _) = await TestClient.LoginAsync(client, u.LoginCode, u.Password);
        Assert.Equal(401, stillLocked);

        // Simulate the lockout window elapsing (deterministic, no real sleep).
        await TestUsers.ClearLockoutAsync(_factory, u.Id);

        // Recovery: the correct password now authenticates successfully.
        var (recovered, body) = await TestClient.LoginAsync(client, u.LoginCode, u.Password);
        Assert.Equal(200, recovered);
        Assert.False(string.IsNullOrEmpty(body!.token));
    }
}
