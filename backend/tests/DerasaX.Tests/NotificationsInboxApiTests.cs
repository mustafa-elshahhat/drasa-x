using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 closure — the current-user notification inbox under the canonical /api/v1 route:
/// list, unread-count, mark-one-read, mark-all-read, with strict per-user scoping (a user can
/// neither read nor mark another user's notifications).
/// </summary>
public class NotificationsInboxApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public NotificationsInboxApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record NotifRow(string id, string title, bool isRead);
    private sealed record UnreadRow(int unreadCount);

    private async Task<List<string>> SeedNotificationsAsync(string userId, int count)
    {
        var ids = new List<string>();
        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        for (var i = 0; i < count; i++)
        {
            var id = $"ntf-{Guid.NewGuid():N}";
            ids.Add(id);
            db.notifications.Add(new Notification
            {
                Id = id, TenantId = "tenant-1", UserId = userId, Title = $"N{i}", Body = "b",
                NotificationCategory = NotificationCategory.General, NotificationType = NotificationType.User,
                IsRead = false, CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        return ids;
    }

    private async Task Cleanup(string userId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" = {0}", userId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"ActorUserId\" = {0}", userId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\" = {0}", userId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\" WHERE \"Id\" = {0}", userId);
    }

    [Fact]
    public async Task Inbox_list_count_mark_read_and_cross_user_isolation()
    {
        var owner = await TestUsers.CreateLockoutStudentAsync(_factory);
        try
        {
            var ids = await SeedNotificationsAsync(owner.Id, 2);
            var client = await TestClient.AuthedClientAsync(_factory, owner.LoginCode);

            // List (canonical v1 route) returns the user's notifications.
            var list = await client.GetAsync("/api/v1/notifications?pageSize=50");
            var rawList = await list.Content.ReadAsStringAsync();
            Assert.True(list.StatusCode == HttpStatusCode.OK, $"status={list.StatusCode} body={rawList}");
            var rows = JsonSerializer.Deserialize<List<NotifRow>>(rawList, Json)!;
            Assert.True(rows.Count >= 2, $"count={rows.Count} body={rawList}");

            // Unread count.
            var count = await client.GetAsync("/api/v1/notifications/unread-count");
            var unread = JsonSerializer.Deserialize<UnreadRow>(await count.Content.ReadAsStringAsync(), Json)!;
            Assert.True(unread.unreadCount >= 2);

            // Mark one read.
            var mark = await client.PatchAsync($"/api/v1/notifications/{ids[0]}/read", null);
            Assert.Equal(HttpStatusCode.NoContent, mark.StatusCode);

            // Another user cannot mark this user's notification → 404 (no cross-user access).
            var other = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            Assert.Equal(HttpStatusCode.NotFound, (await other.PatchAsync($"/api/v1/notifications/{ids[1]}/read", null)).StatusCode);

            // Mark all read.
            Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsync("/api/v1/notifications/read-all", null)).StatusCode);
            var after = JsonSerializer.Deserialize<UnreadRow>(
                await (await client.GetAsync("/api/v1/notifications/unread-count")).Content.ReadAsStringAsync(), Json)!;
            Assert.Equal(0, after.unreadCount);
        }
        finally { await Cleanup(owner.Id); }
    }
}
