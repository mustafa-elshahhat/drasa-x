using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 13 — notification preferences + announcement-targeting routing + honest delivery state.
/// Uses a throwaway small tenant so the announcement fan-out is bounded and deterministic (the shared
/// seed inflates tenant-1 to ~1940 students). Every created row is cleaned up in a finally block.
/// </summary>
public class Phase13NotificationRoutingTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase13NotificationRoutingTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record NotifRow(string id, string title, NotificationCategory category, NotificationChannelStatus emailStatus, bool isRead);
    private sealed record UnreadRow(int unreadCount);
    private sealed record PrefRow(NotificationCategory category, bool inAppEnabled, bool emailEnabled, bool mandatory, bool emailConfigured);
    private sealed record Envelope<T>(T data, bool success, int statusCode);
    private sealed record IdRow(string id);

    private async Task<(string id, string loginCode)> NewUserAsync<TUser>(string tenantId, string role, string? gradeId = null)
        where TUser : ApplicationUser, new()
    {
        var loginCode = $"{role[..3].ToUpperInvariant()}-{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));

        var user = new TUser { UserName = loginCode.ToLowerInvariant(), FullName = $"PH13 {role}", LoginCode = loginCode, TenantId = tenantId };
        if (user is Student s && gradeId is not null) s.GradeId = gradeId;
        var res = await users.CreateAsync(user, TestClient.Password);
        if (!res.Succeeded) throw new Exception($"create {role}: {string.Join(",", res.Errors)}");
        await users.AddToRoleAsync(user, role);
        return (user.Id, loginCode);
    }

    private static async Task<int> UnreadAsync(HttpClient c)
    {
        var r = await c.GetAsync("/api/v1/notifications/unread-count");
        return JsonSerializer.Deserialize<UnreadRow>(await r.Content.ReadAsStringAsync(), Json)!.unreadCount;
    }

    private static async Task<List<NotifRow>> ListAsync(HttpClient c)
    {
        var r = await c.GetAsync("/api/v1/notifications?pageSize=100");
        return JsonSerializer.Deserialize<List<NotifRow>>(await r.Content.ReadAsStringAsync(), Json)!;
    }

    [Fact]
    public async Task Announcement_targeting_routes_to_recipients_only_and_honours_preferences()
    {
        var tenantId = $"ph13-{Guid.NewGuid():N}"[..18];
        await Phase4Db.EnsureTenantAsync(_factory, tenantId);
        var (adminId, adminCode) = await NewUserAsync<SchoolAdmin>(tenantId, "SchoolAdmin");
        var (stuAId, stuACode) = await NewUserAsync<Student>(tenantId, "Student", "G7-ID");
        var (stuBId, stuBCode) = await NewUserAsync<Student>(tenantId, "Student", "G7-ID");
        var (teaId, teaCode) = await NewUserAsync<Teacher>(tenantId, "Teacher");
        try
        {
            var admin = await TestClient.AuthedClientAsync(_factory, adminCode);
            var stuA = await TestClient.AuthedClientAsync(_factory, stuACode);
            var stuB = await TestClient.AuthedClientAsync(_factory, stuBCode);
            var teacher = await TestClient.AuthedClientAsync(_factory, teaCode);

            var beforeA = await UnreadAsync(stuA);

            // Student B opts OUT of the (optional) Announcement category.
            var prefResp = await stuB.PutAsJsonAsync("/api/v1/notification-preferences",
                new { category = (int)NotificationCategory.Announcement, inAppEnabled = false, emailEnabled = false });
            Assert.Equal(HttpStatusCode.OK, prefResp.StatusCode);

            // Admin creates + publishes a Students-only announcement.
            var create = await admin.PostAsJsonAsync("/api/v1/announcements",
                new { title = "PH13 Students Notice", body = "Students-only broadcast.", targetAudience = (int)TargetAudience.Students });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var annId = JsonSerializer.Deserialize<Envelope<IdRow>>(await create.Content.ReadAsStringAsync(), Json)!.data.id;
            var publish = await admin.PostAsync($"/api/v1/announcements/{annId}/publish?publish=true", null);
            Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

            // Student A (targeted, default prefs) receives an Announcement notification.
            var listA = await ListAsync(stuA);
            Assert.Contains(listA, n => n.title == "PH13 Students Notice" && n.category == NotificationCategory.Announcement);
            Assert.True(await UnreadAsync(stuA) > beforeA, "targeted student unread count must increase");

            // Honest delivery state: in-app delivered, e-mail NotConfigured (never faked).
            var ann = listA.Find(n => n.title == "PH13 Students Notice")!;
            Assert.Equal(NotificationChannelStatus.NotConfigured, ann.emailStatus);

            // Student B suppressed the category → NO announcement notification.
            Assert.DoesNotContain(await ListAsync(stuB), n => n.title == "PH13 Students Notice");

            // Teacher is NOT in the targeted audience → recipients-only (no leak).
            Assert.DoesNotContain(await ListAsync(teacher), n => n.title == "PH13 Students Notice");
        }
        finally { await CleanupTenantAsync(tenantId, adminId, stuAId, stuBId, teaId); }
    }

    [Fact]
    public async Task Preferences_list_defaults_and_mandatory_cannot_be_disabled()
    {
        var tenantId = $"ph13-{Guid.NewGuid():N}"[..18];
        await Phase4Db.EnsureTenantAsync(_factory, tenantId);
        var (stuId, stuCode) = await NewUserAsync<Student>(tenantId, "Student", "G7-ID");
        try
        {
            var stu = await TestClient.AuthedClientAsync(_factory, stuCode);

            // GET lists every category; Warning is mandatory + enabled, Announcement is optional.
            var prefs = JsonSerializer.Deserialize<Envelope<List<PrefRow>>>(await (await stu.GetAsync("/api/v1/notification-preferences")).Content.ReadAsStringAsync(), Json)!.data;
            Assert.Contains(prefs, p => p.category == NotificationCategory.Warning && p.mandatory && p.inAppEnabled);
            Assert.Contains(prefs, p => p.category == NotificationCategory.Announcement && !p.mandatory);
            Assert.All(prefs, p => Assert.False(p.emailConfigured)); // e-mail honestly not configured

            // Disabling an optional category succeeds.
            Assert.Equal(HttpStatusCode.OK, (await stu.PutAsJsonAsync("/api/v1/notification-preferences",
                new { category = (int)NotificationCategory.QuizGraded, inAppEnabled = false, emailEnabled = false })).StatusCode);

            // Disabling a MANDATORY category is rejected (400) — essential notices can't be suppressed.
            Assert.Equal(HttpStatusCode.BadRequest, (await stu.PutAsJsonAsync("/api/v1/notification-preferences",
                new { category = (int)NotificationCategory.Warning, inAppEnabled = false, emailEnabled = false })).StatusCode);

            // Unauthenticated → 401.
            var anon = TestClient.NewClient(_factory);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/v1/notification-preferences")).StatusCode);
        }
        finally { await CleanupTenantAsync(tenantId, stuId); }
    }

    private async Task CleanupTenantAsync(string tenantId, params string[] userIds)
    {
        await using (var db = Phase4Db.Platform(_factory))
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notificationPreferences\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"announcements\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"TenantId\" = {0}", tenantId);
        }
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var uid in userIds)
            {
                var u = await users.FindByIdAsync(uid);
                if (u is not null) await users.DeleteAsync(u);
            }
        }
        await using (var db = Phase4Db.Platform(_factory))
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM tenants WHERE \"Id\" = {0}", tenantId);
        }
    }
}
