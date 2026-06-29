using System;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Phase 4 §6.7 — engagement domain/database integrity.</summary>
public class EngagementDomainTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public EngagementDomainTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<string> UserId(DerasaXDbContext db, string loginCode) =>
        (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;

    [Fact]
    public async Task Community_post_comment_and_report_are_tenant_safe()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var studentT1 = await UserId(setup, "STU-T1");
        var studentT2 = await UserId(setup, "STU-T2");
        var communityId = Phase4Db.NewId("com");
        var membershipId = Phase4Db.NewId("mem");
        var postId = Phase4Db.NewId("post");
        var commentId = Phase4Db.NewId("pc");
        var reportId = Phase4Db.NewId("pr");

        try
        {
            await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                db.communities.Add(new Community { Id = communityId, TenantId = "tenant-1", Name = Phase4Db.NewId("Community") });
                await db.SaveChangesAsync();
            }

            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                bad.communityMemberships.Add(new CommunityMembership { Id = Phase4Db.NewId("mem"), TenantId = "tenant-1", CommunityId = communityId, UserId = studentT2 });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            await using (var good = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                good.communityMemberships.Add(new CommunityMembership { Id = membershipId, TenantId = "tenant-1", CommunityId = communityId, UserId = studentT1, Role = CommunityMemberRole.Member });
                good.posts.Add(new Post { Id = postId, TenantId = "tenant-1", CommunityId = communityId, UserId = studentT1, Content = "Hello" });
                good.postComments.Add(new PostComment { Id = commentId, TenantId = "tenant-1", PostId = postId, UserId = studentT1, Body = "Nice" });
                good.postReports.Add(new PostReport { Id = reportId, TenantId = "tenant-1", PostId = postId, ReportedByUserId = studentT1, Reason = "Smoke" });
                await good.SaveChangesAsync();
                Assert.True(await good.postComments.AnyAsync(x => x.PostId == postId));
            }
        }
        finally
        {
            await CleanupAsync("postReports", reportId);
            await CleanupAsync("postComments", commentId);
            await CleanupAsync("posts", postId);
            await CleanupAsync("communityMemberships", membershipId);
            await CleanupAsync("communities", communityId);
        }
    }

    [Fact]
    public async Task Competition_badge_streak_and_office_hour_records_succeed()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var studentT1 = await UserId(setup, "STU-T1");
        var teacherT1 = await UserId(setup, "TEACH-T1");
        var competitionId = Phase4Db.NewId("comp");
        var entryId = Phase4Db.NewId("cent");
        var scoreId = Phase4Db.NewId("cscore");
        var leaderboardId = Phase4Db.NewId("lead");
        var badgeId = Phase4Db.NewId("badge");
        var studentBadgeId = Phase4Db.NewId("sbadge");
        var streakId = Phase4Db.NewId("str");
        var officeId = Phase4Db.NewId("oh");
        var bookingId = Phase4Db.NewId("ohb");

        try
        {
            await using (var platform = Phase4Db.Platform(_factory))
            {
                platform.badges.Add(new Badge { Id = badgeId, Code = Phase4Db.NewId("BDG"), Name = "Starter", Type = BadgeType.Achievement });
                await platform.SaveChangesAsync();
            }

            await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
            db.competitions.Add(new Competition { Id = competitionId, TenantId = "tenant-1", Title = "Contest", Status = CompetitionStatus.Active, StartsAt = DateTime.UtcNow, EndsAt = DateTime.UtcNow.AddDays(1) });
            db.competitionEntries.Add(new CompetitionEntry { Id = entryId, TenantId = "tenant-1", CompetitionId = competitionId, StudentId = studentT1 });
            db.competitionScores.Add(new CompetitionScore { Id = scoreId, TenantId = "tenant-1", CompetitionEntryId = entryId, Score = 99m, Rank = 1 });
            db.leaderboardEntries.Add(new LeaderboardEntry { Id = leaderboardId, TenantId = "tenant-1", CompetitionId = competitionId, StudentId = studentT1, Score = 99m, Rank = 1 });
            db.studentBadges.Add(new StudentBadge { Id = studentBadgeId, TenantId = "tenant-1", StudentId = studentT1, BadgeId = badgeId });
            db.studentStreaks.Add(new StudentStreak { Id = streakId, TenantId = "tenant-1", StudentId = studentT1, CurrentCount = 3, LongestCount = 3, LastActivityDate = DateTime.UtcNow.Date });
            db.officeHourSessions.Add(new OfficeHourSession { Id = officeId, TenantId = "tenant-1", TeacherId = teacherT1, Title = "Help", StartsAt = DateTime.UtcNow.AddHours(1), EndsAt = DateTime.UtcNow.AddHours(2), Capacity = 5 });
            db.officeHourBookings.Add(new OfficeHourBooking { Id = bookingId, TenantId = "tenant-1", OfficeHourSessionId = officeId, StudentId = studentT1, Status = OfficeHourBookingStatus.Confirmed });
            await db.SaveChangesAsync();

            Assert.True(await db.leaderboardEntries.AnyAsync(x => x.CompetitionId == competitionId));
            Assert.True(await db.officeHourBookings.AnyAsync(x => x.OfficeHourSessionId == officeId));
        }
        finally
        {
            await CleanupAsync("officeHourBookings", bookingId);
            await CleanupAsync("officeHourSessions", officeId);
            await CleanupAsync("studentStreaks", streakId);
            await CleanupAsync("studentBadges", studentBadgeId);
            await CleanupAsync("leaderboardEntries", leaderboardId);
            await CleanupAsync("competitionScores", scoreId);
            await CleanupAsync("competitionEntries", entryId);
            await CleanupAsync("competitions", competitionId);
            await CleanupAsync("badges", badgeId);
        }
    }

    private async Task CleanupAsync(string set, string id)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"" + set + "\" WHERE \"Id\" = {0}", id);
    }
}
