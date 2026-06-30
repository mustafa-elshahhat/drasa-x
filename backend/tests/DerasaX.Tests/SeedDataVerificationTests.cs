using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Verifies the development seed catalog is realistic, connected, and free of
/// placeholder/phase/test display names. Runs against the migrated + seeded local
/// PostgreSQL database (the same one the integration suite uses). These tests fail
/// loudly if a reseed regresses the demo experience: empty dashboards or raw
/// placeholder strings ("Phase 8 …", "Tenant1 …", "Main School", "Grade3", …).
/// </summary>
public class SeedDataVerificationTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public SeedDataVerificationTests(IntegrationFactory factory) => _factory = factory;

    // Substrings that must never appear in a user-facing seeded display field.
    private static readonly string[] Forbidden =
    {
        "Phase 1", "Phase 2", "Phase 3", "Phase 4", "Phase 5", "Phase 6", "Phase 7", "Phase 8", "Phase 9",
        "Tenant1", "Tenant2", "Main School", "North Academy", "Suspended School", "Grade3", "Lorem"
    };

    // Id prefixes used by the seed catalog (test-created rows use random GUID ids,
    // so scoping by these prefixes excludes transient test data from the scan).
    private static readonly string[] SeedIdPrefixes =
    {
        "tenant-", "G7-ID", "G8-ID", "T2-", "SUS-", "PH8-", "PH9-", "PH10-", "PH11-", "PH12-", "PH13-", "E2E-PH", "SHOW-"
    };

    private static bool IsSeedId(string? id) => id != null && SeedIdPrefixes.Any(p => id.StartsWith(p, StringComparison.Ordinal));

    private static void AssertClean(string label, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        foreach (var bad in Forbidden)
            Assert.False(value.Contains(bad, StringComparison.OrdinalIgnoreCase),
                $"Seeded {label} contains placeholder text '{bad}': \"{value}\"");
    }

    [Fact]
    public async Task Seed_tenants_have_realistic_names()
    {
        await using var db = Phase4Db.Platform(_factory);
        var tenants = await db.tenants.IgnoreQueryFilters().ToListAsync();

        Assert.Equal("Nile Future International School", tenants.Single(t => t.Id == "tenant-1").Name);
        Assert.Equal("Al-Nahda STEM School", tenants.Single(t => t.Id == "tenant-2").Name);
        // A realistic suspended tenant for the auth/security gate.
        Assert.Equal(TenantStatus.Suspended, tenants.Single(t => t.Id == "tenant-suspended").Status);

        foreach (var t in tenants.Where(t => IsSeedId(t.Id)))
            AssertClean($"tenant '{t.Id}' name", t.Name);
    }

    [Fact]
    public async Task Seed_curriculum_has_realistic_names()
    {
        await using var db = Phase4Db.Platform(_factory);

        Assert.Equal("Grade 7", (await db.grades.IgnoreQueryFilters().FirstAsync(g => g.Id == "G7-ID")).Name);
        Assert.Equal("Grade 8", (await db.grades.IgnoreQueryFilters().FirstAsync(g => g.Id == "G8-ID")).Name);
        Assert.Equal("Mathematics", (await db.subjects.IgnoreQueryFilters().FirstAsync(s => s.Id == "PH8-SUBJECT-T1")).Name);
        Assert.Equal("Algebra", (await db.units.IgnoreQueryFilters().FirstAsync(u => u.Id == "PH8-UNIT-T1")).Title);
        Assert.Equal("Linear Equations", (await db.lessons.IgnoreQueryFilters().FirstAsync(l => l.Id == "PH8-LESSON-T1")).Title);
        Assert.Equal("Grade 7 - A", (await db.schoolClasses.IgnoreQueryFilters().FirstAsync(c => c.Id == "PH8-CLASS-T1")).Name);

        foreach (var g in (await db.grades.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"grade '{g.Id}'", g.Name);
        foreach (var s in (await db.subjects.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"subject '{s.Id}'", s.Name);
        foreach (var u in (await db.units.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"unit '{u.Id}'", u.Title);
        foreach (var l in (await db.lessons.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"lesson '{l.Id}'", l.Title);
        foreach (var c in (await db.schoolClasses.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"class '{c.Id}'", c.Name);
    }

    [Fact]
    public async Task Seed_role_accounts_have_realistic_names()
    {
        await using var db = Phase4Db.Platform(_factory);
        var users = await db.Users.IgnoreQueryFilters().ToListAsync();

        string Name(string loginCode) => users.Single(u => u.LoginCode == loginCode).FullName;
        Assert.Equal("Youssef Ibrahim", Name("STU-T1"));   // Student
        Assert.Equal("Karim Adel", Name("TEACH-T1"));      // Teacher
        Assert.Equal("Hassan Fathy", Name("PH10-PARENT-T1")); // Parent (linked to STU-T1)
        Assert.Equal("Hala Mansour", Name("ADMIN-T1"));    // SchoolAdmin
        Assert.Equal("Adham Roushdy", Name("SYS-1"));      // SystemAdmin

        // Every standing seed account (login codes the demo + matrix use) reads as a real person.
        var seedLoginPrefixes = new[] { "STU-", "TEACH-", "ADMIN-", "PARENT-", "SYS-", "PH8-", "PH10-", "PH11-", "PH12-", "PH13-", "ST-00", "PARENT-DEMO" };
        foreach (var u in users.Where(u => u.LoginCode != null && seedLoginPrefixes.Any(p => u.LoginCode.StartsWith(p, StringComparison.Ordinal))))
        {
            Assert.False(string.IsNullOrWhiteSpace(u.FullName), $"Seed user {u.LoginCode} has no full name");
            AssertClean($"user '{u.LoginCode}' full name", u.FullName);
        }
    }

    [Fact]
    public async Task Showcase_student_lands_on_non_empty_connected_pages()
    {
        await using var db = Phase4Db.Platform(_factory);
        var now = DateTime.UtcNow;

        var omar = (await db.Users.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == "ST-001")).Id;

        // ---- Homework: >= 3 records visible in every category ----
        var subs = await db.assignmentSubmissions.IgnoreQueryFilters().Where(s => s.StudentId == omar).ToListAsync();
        Assert.True(subs.Count(s => s.Status == SubmissionStatus.Submitted) >= 3, "expected >= 3 submitted homework");
        Assert.True(subs.Count(s => s.Status == SubmissionStatus.Graded) >= 3, "expected >= 3 graded homework");
        Assert.True(subs.Count(s => s.Status == SubmissionStatus.Late) >= 3, "expected >= 3 late homework");
        Assert.All(subs.Where(s => s.Status == SubmissionStatus.Graded), s => Assert.NotNull(s.Score));

        // Assigned (pending): class-targeted, published, future-due homework with no submission.
        var classAsgIds = await db.assignmentTargets.IgnoreQueryFilters()
            .Where(t => t.SchoolClassId == "SHOW-CLASS-T1" && t.TargetType == AssignmentTargetType.Class)
            .Select(t => t.AssignmentId).ToListAsync();
        var hw = await db.assignments.IgnoreQueryFilters()
            .Where(a => classAsgIds.Contains(a.Id) && a.Type == AssignmentType.Homework && a.Status == AssignmentStatus.Published)
            .ToListAsync();
        var omarSubmittedAsg = subs.Select(s => s.AssignmentId).ToHashSet();
        Assert.True(hw.Count(a => a.DueDate > now && !omarSubmittedAsg.Contains(a.Id)) >= 3, "expected >= 3 assigned (pending) homework");

        // ---- Quiz: a completed, scored attempt ----
        var attempt = await db.quizSubmissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(q => q.StudentId == omar && q.QuizId == "SHOW-QUIZ-1");
        Assert.NotNull(attempt);
        Assert.Equal(SubmissionStatus.Graded, attempt!.submissionStatus);
        Assert.Equal(2, attempt.AchievedScore);
        Assert.Equal(3, attempt.TotalScore);

        // ---- Progress / attendance / notifications / badges / streak / parent link ----
        Assert.True(await db.subjectProgresses.IgnoreQueryFilters().AnyAsync(p => p.StudentId == omar), "expected subject progress");
        Assert.True(await db.studentAttendanceRecords.IgnoreQueryFilters().CountAsync(a => a.StudentId == omar) >= 3, "expected >= 3 attendance records");
        Assert.True(await db.notifications.IgnoreQueryFilters().CountAsync(n => n.UserId == omar && !n.IsRead) >= 2, "expected >= 2 unread notifications");
        Assert.True(await db.studentBadges.IgnoreQueryFilters().AnyAsync(b => b.StudentId == omar), "expected an earned badge");
        Assert.True(await db.studentStreaks.IgnoreQueryFilters().AnyAsync(s => s.StudentId == omar), "expected a study streak");
        Assert.True(await db.parentStudentRelationships.IgnoreQueryFilters().AnyAsync(r => r.StudentId == omar && r.IsActive), "expected a linked parent");
    }

    [Fact]
    public async Task Seed_display_text_has_no_placeholder_substrings()
    {
        await using var db = Phase4Db.Platform(_factory);

        foreach (var a in (await db.assignments.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"assignment '{a.Id}'", a.Title);
        foreach (var q in (await db.quizzes.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"quiz '{q.Id}'", q.Title);
        foreach (var c in (await db.communities.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"community '{c.Id}'", c.Name);
        foreach (var c in (await db.competitions.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"competition '{c.Id}'", c.Title);
        foreach (var an in (await db.announcements.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
            AssertClean($"announcement '{an.Id}'", an.Title);
        foreach (var n in (await db.notifications.IgnoreQueryFilters().ToListAsync()).Where(x => IsSeedId(x.Id)))
        {
            AssertClean($"notification '{n.Id}' title", n.Title);
            AssertClean($"notification '{n.Id}' body", n.Body);
        }
    }
}
