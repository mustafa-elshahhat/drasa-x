using System;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Phase 4 §6.5 — progress and insights domain/database integrity.</summary>
public class ProgressInsightsDomainTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public ProgressInsightsDomainTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<string> UserId(DerasaXDbContext db, string loginCode) =>
        (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;

    [Fact]
    public async Task Cross_tenant_progress_student_is_rejected_and_same_tenant_succeeds()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var stuT1 = await UserId(setup, "STU-T1");
        var stuT2 = await UserId(setup, "STU-T2");
        var progressId = Phase4Db.NewId("slp");
        var subjectId = Phase4Db.NewId("subj");
        var unitId = Phase4Db.NewId("unit");
        var lessonId = Phase4Db.NewId("les");

        try
        {
            await using (var seed = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                seed.subjects.Add(new Subject { Id = subjectId, TenantId = "tenant-1", Name = "Progress Subject", GradeId = "G7-ID" });
                seed.units.Add(new Unit { Id = unitId, TenantId = "tenant-1", Title = "Progress Unit", SubjectId = subjectId });
                seed.lessons.Add(new Lesson { Id = lessonId, TenantId = "tenant-1", Title = "Progress Lesson", Content = "x", UnitId = unitId });
                await seed.SaveChangesAsync();
            }

            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                bad.studentLessonProgresses.Add(new StudentLessonProgress
                {
                    Id = Phase4Db.NewId("slp"), TenantId = "tenant-1", StudentId = stuT2,
                    LessonId = lessonId, CompletionPercentage = 25m
                });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            await using (var good = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                good.studentLessonProgresses.Add(new StudentLessonProgress
                {
                    Id = progressId, TenantId = "tenant-1", StudentId = stuT1,
                    LessonId = lessonId, CompletionPercentage = 100m, IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                });
                await good.SaveChangesAsync();
                Assert.True(await good.studentLessonProgresses.AnyAsync(x => x.Id == progressId));
            }
        }
        finally
        {
            await CleanupAsync("studentLessonProgresses", progressId);
            await CleanupAsync("lessons", lessonId);
            await CleanupAsync("units", unitId);
            await CleanupAsync("subjects", subjectId);
        }
    }

    [Fact]
    public async Task Insight_can_own_pain_points_recommendations_predictions_and_metrics()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var stuT1 = await UserId(setup, "STU-T1");
        var insightId = Phase4Db.NewId("ins");
        var painId = Phase4Db.NewId("pain");
        var recId = Phase4Db.NewId("rec");
        var predId = Phase4Db.NewId("pred");
        var metricId = Phase4Db.NewId("met");

        try
        {
            await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
            db.studentInsights.Add(new StudentInsight
            {
                Id = insightId, TenantId = "tenant-1", StudentId = stuT1,
                PeriodStart = DateTime.UtcNow.Date, PeriodEnd = DateTime.UtcNow.Date.AddDays(7),
                Performance = PerformanceLevel.OnTrack, ConfidenceScore = 0.90m, Summary = "Stable"
            });
            db.painPoints.Add(new PainPoint
            {
                Id = painId, TenantId = "tenant-1", StudentId = stuT1, StudentInsightId = insightId,
                Category = PainPointCategory.Concept, Title = "Fractions", ConfidenceScore = 0.80m
            });
            db.studentRecommendations.Add(new StudentRecommendation
            {
                Id = recId, TenantId = "tenant-1", StudentId = stuT1, StudentInsightId = insightId,
                Title = "Practice", Body = "Review fraction exercises."
            });
            db.predictionRecords.Add(new PredictionRecord
            {
                Id = predId, TenantId = "tenant-1", StudentId = stuT1,
                Kind = PredictionKind.Performance, PredictedScore = 88m,
                Level = PerformanceLevel.OnTrack, ConfidenceScore = 0.84m
            });
            db.studentMetricHistories.Add(new StudentMetricHistory
            {
                Id = metricId, TenantId = "tenant-1", StudentId = stuT1,
                MetricType = ProgressMetricType.QuizScore, Value = 88m
            });
            await db.SaveChangesAsync();

            Assert.True(await db.painPoints.AnyAsync(x => x.StudentInsightId == insightId));
            Assert.True(await db.studentRecommendations.AnyAsync(x => x.StudentInsightId == insightId));
            Assert.True(await db.predictionRecords.AnyAsync(x => x.StudentId == stuT1));
            Assert.True(await db.studentMetricHistories.AnyAsync(x => x.StudentId == stuT1));
        }
        finally
        {
            await CleanupAsync("studentMetricHistories", metricId);
            await CleanupAsync("predictionRecords", predId);
            await CleanupAsync("studentRecommendations", recId);
            await CleanupAsync("painPoints", painId);
            await CleanupAsync("studentInsights", insightId);
        }
    }

    private async Task CleanupAsync(string set, string id)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"" + set + "\" WHERE \"Id\" = {0}", id);
    }
}
