using System;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Phase 4 §6.4 — assessment domain/database integrity.</summary>
public class AssessmentDomainTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AssessmentDomainTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<string> UserId(DerasaXDbContext db, string loginCode) =>
        (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;

    private async Task<string> CreateQuizAsync(string tenant)
    {
        var id = Phase4Db.NewId("quiz");
        await using var db = Phase4Db.AsTenant(_factory, tenant);
        db.quizzes.Add(new Quiz
        {
            Id = id, TenantId = tenant, Title = "T", Status = QuizStatus.AiGenerated, Origin = QuizOrigin.AiGenerated
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task Quiz_lifecycle_ai_draft_to_teacher_approved()
    {
        var quizId = await CreateQuizAsync("tenant-1");
        try
        {
            await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var q = await db.quizzes.FirstAsync(x => x.Id == quizId);
                Assert.Equal(QuizStatus.AiGenerated, q.Status);
                q.Status = QuizStatus.Approved;
                q.ApprovedByTeacherId = await UserId(db, "TEACH-T1");
                q.ApprovedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            await using (var verify = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var q = await verify.quizzes.FirstAsync(x => x.Id == quizId);
                Assert.Equal(QuizStatus.Approved, q.Status);
                Assert.NotNull(q.ApprovedAt);
            }
        }
        finally { await CleanupAsync("quizzes", quizId); }
    }

    [Fact]
    public async Task Cross_tenant_question_is_rejected()
    {
        var quizId = await CreateQuizAsync("tenant-1");
        try
        {
            await using var bad = Phase4Db.AsTenant(_factory, "tenant-2");
            // Question claims tenant-2 but points at a tenant-1 quiz -> composite FK fails.
            bad.questions.Add(new Question { Id = Phase4Db.NewId("q"), TenantId = "tenant-2", Text = "x", QuizId = quizId, Order = 1 });
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
        }
        finally { await CleanupAsync("quizzes", quizId); }
    }

    [Fact]
    public async Task Cross_tenant_submission_student_rejected_same_tenant_ok_and_attempt_history()
    {
        var quizId = await CreateQuizAsync("tenant-1");
        await using var setup = Phase4Db.Platform(_factory);
        var stuT1 = await UserId(setup, "STU-T1");
        var stuT2 = await UserId(setup, "STU-T2");
        var subIds = new System.Collections.Generic.List<string>();
        try
        {
            // Cross-tenant student submission -> rejected by trigger.
            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                bad.quizSubmissions.Add(new QuizSubmission { Id = Phase4Db.NewId("sub"), TenantId = "tenant-1", QuizId = quizId, StudentId = stuT2, AttemptNumber = 1 });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            // Same-tenant: attempt 1 then attempt 2 (history).
            await using (var a1 = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var s = new QuizSubmission { Id = Phase4Db.NewId("sub"), TenantId = "tenant-1", QuizId = quizId, StudentId = stuT1, AttemptNumber = 1, IsLatestAttempt = false };
                a1.quizSubmissions.Add(s); await a1.SaveChangesAsync(); subIds.Add(s.Id);
            }
            await using (var a2 = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var s = new QuizSubmission { Id = Phase4Db.NewId("sub"), TenantId = "tenant-1", QuizId = quizId, StudentId = stuT1, AttemptNumber = 2, IsLatestAttempt = true };
                a2.quizSubmissions.Add(s); await a2.SaveChangesAsync(); subIds.Add(s.Id);
            }

            // Duplicate attempt number for same student/quiz -> unique violation.
            await using (var dup = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                dup.quizSubmissions.Add(new QuizSubmission { Id = Phase4Db.NewId("sub"), TenantId = "tenant-1", QuizId = quizId, StudentId = stuT1, AttemptNumber = 1 });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => dup.SaveChangesAsync());
            }

            await using (var verify = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var count = await verify.quizSubmissions.CountAsync(x => x.QuizId == quizId && x.StudentId == stuT1);
                Assert.Equal(2, count); // attempt history preserved
            }
        }
        finally
        {
            foreach (var id in subIds) await CleanupAsync("quizSubmissions", id);
            await CleanupAsync("quizzes", quizId);
        }
    }

    [Fact]
    public async Task Quiz_with_submissions_cannot_be_cascade_deleted()
    {
        var quizId = await CreateQuizAsync("tenant-1");
        await using var setup = Phase4Db.Platform(_factory);
        var stuT1 = await UserId(setup, "STU-T1");
        string subId = Phase4Db.NewId("sub");
        try
        {
            await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                db.quizSubmissions.Add(new QuizSubmission { Id = subId, TenantId = "tenant-1", QuizId = quizId, StudentId = stuT1, AttemptNumber = 1 });
                await db.SaveChangesAsync();
            }
            // Deleting the quiz while a submission (grade history) exists is RESTRICTed.
            await using (var del = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var q = await del.quizzes.FirstAsync(x => x.Id == quizId);
                del.quizzes.Remove(q);
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => del.SaveChangesAsync());
            }
        }
        finally
        {
            await CleanupAsync("quizSubmissions", subId);
            await CleanupAsync("quizzes", quizId);
        }
    }

    private async Task CleanupAsync(string set, string id)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"" + set + "\" WHERE \"Id\" = {0}", id);
    }
}
