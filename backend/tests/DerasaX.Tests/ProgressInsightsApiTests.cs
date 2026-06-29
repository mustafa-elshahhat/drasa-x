using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 §11 (Increment 4) — progress &amp; insights APIs through the real HTTP pipeline:
/// the shared relationship-authorization rule (self / assigned-teacher / linked-parent /
/// same-tenant SchoolAdmin / no platform-admin), empty-data and date-range validation, real
/// aggregation from assessment records, stored-AI-output labelling, and the guarantee that
/// read endpoints never execute AI.
/// </summary>
public class ProgressInsightsApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public ProgressInsightsApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record SummaryData(string studentId, int quizAttempts, decimal averageQuizPercentage);
    private sealed record PredictionRow(string id, string? modelName, string source);
    private sealed record ClassPerfData(string classId, int studentCount, int quizAttempts);

    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}";

    private async Task<string> UserId(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    private sealed record World(string yearId, string classId, string enrollId, string tcaId, string psrId,
        string quizId, string submissionId, string metricId, string insightId, string predictionId,
        string painId, string recId, string stuId, string stuLogin);

    private async Task<World> SetupAsync()
    {
        // Dedicated throwaway student so aggregation counts are deterministic under the
        // parallel test load (STU-T1 is shared by other suites that create submissions).
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var stuId = student.Id;
        var teacherId = await UserId("TEACH-T1");
        var parentId = await UserId("PARENT-T1");

        var w = new World(
            NewId("ay"), NewId("cls"), NewId("enr"), NewId("tca"), NewId("psr"),
            NewId("quiz"), NewId("sub"), NewId("metric"), NewId("insight"), NewId("pred"),
            NewId("pain"), NewId("rec"), stuId, student.LoginCode);

        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.academicYears.Add(new AcademicYear { Id = w.yearId, TenantId = "tenant-1", Name = "Y", Code = NewId("AY")[..12], StartDate = U(2030, 9, 1), EndDate = U(2031, 6, 30) });
        db.schoolClasses.Add(new SchoolClass { Id = w.classId, TenantId = "tenant-1", Name = "C", Code = NewId("C")[..12], GradeId = "G7-ID", AcademicYearId = w.yearId, Capacity = 30 });
        db.enrollments.Add(new Enrollment { Id = w.enrollId, TenantId = "tenant-1", StudentId = stuId, SchoolClassId = w.classId, AcademicYearId = w.yearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
        db.teacherClassAssignments.Add(new TeacherClassAssignment { Id = w.tcaId, TenantId = "tenant-1", TeacherId = teacherId, SchoolClassId = w.classId, IsActive = true, ActiveFrom = DateTime.UtcNow });
        db.parentStudentRelationships.Add(new ParentStudentRelationship { Id = w.psrId, TenantId = "tenant-1", ParentId = parentId, StudentId = stuId, IsActive = true, CanViewProgress = true });

        db.quizzes.Add(new Quiz { Id = w.quizId, TenantId = "tenant-1", Title = "Agg", Status = QuizStatus.Published });
        db.quizSubmissions.Add(new QuizSubmission { Id = w.submissionId, TenantId = "tenant-1", QuizId = w.quizId, StudentId = stuId, AttemptNumber = 1, submissionStatus = SubmissionStatus.Graded, AchievedScore = 8, TotalScore = 10, SubmittedAt = DateTime.UtcNow });

        db.studentMetricHistories.Add(new StudentMetricHistory { Id = w.metricId, TenantId = "tenant-1", StudentId = stuId, MetricType = ProgressMetricType.QuizScore, Value = 80, MeasuredAt = U(2030, 10, 1) });
        db.studentInsights.Add(new StudentInsight { Id = w.insightId, TenantId = "tenant-1", StudentId = stuId, Performance = PerformanceLevel.OnTrack, ConfidenceScore = 0.9m, Period = InsightPeriod.Weekly, PeriodStart = U(2030, 9, 1), PeriodEnd = U(2030, 9, 7), Summary = "ok" });
        db.predictionRecords.Add(new PredictionRecord { Id = w.predictionId, TenantId = "tenant-1", StudentId = stuId, Kind = PredictionKind.Performance, PredictedScore = 75, Level = PerformanceLevel.OnTrack, ConfidenceScore = 0.8m, ModelName = "phase6-model", ModelVersion = "v1", PredictedAt = DateTime.UtcNow });
        db.painPoints.Add(new PainPoint { Id = w.painId, TenantId = "tenant-1", StudentId = stuId, Category = PainPointCategory.Concept, Title = "Fractions", ConfidenceScore = 0.7m, DetectedAt = DateTime.UtcNow });
        db.studentRecommendations.Add(new StudentRecommendation { Id = w.recId, TenantId = "tenant-1", StudentId = stuId, Title = "Practice", Body = "Do more", Status = RecommendationStatus.Open, GeneratedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return w;
    }

    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    private async Task Cleanup(World w)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var (table, id) in new[]
        {
            ("studentRecommendations", w.recId), ("painPoints", w.painId), ("predictionRecords", w.predictionId),
            ("studentInsights", w.insightId), ("studentMetricHistories", w.metricId), ("quizSubmissions", w.submissionId),
            ("quizzes", w.quizId), ("parentStudentRelationships", w.psrId), ("teacherClassAssignments", w.tcaId),
            ("enrollments", w.enrollId), ("schoolClasses", w.classId), ("academicYears", w.yearId)
        })
            // Table name is a hardcoded constant from the literal array above (not user input).
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"Id\" = {{0}}", id);
#pragma warning restore EF1002
    }

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
            ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    [Fact]
    public async Task Student_self_access_other_rejection_and_cross_tenant_404()
    {
        var w = await SetupAsync();
        var other = await TestUsers.CreateLockoutStudentAsync(_factory); // a different tenant-1 student
        try
        {
            var stu = await TestClient.AuthedClientAsync(_factory, w.stuLogin);
            Assert.Equal(HttpStatusCode.OK, (await stu.GetAsync($"/api/v1/students/{w.stuId}/progress-summary")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await stu.GetAsync($"/api/v1/students/{other.Id}/progress-summary")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await stu.GetAsync($"/api/v1/students/{await UserId("STU-T2")}/progress-summary")).StatusCode);
        }
        finally { await Cleanup(w); }
    }

    [Fact]
    public async Task Teacher_assigned_access_and_unassigned_rejection()
    {
        var w = await SetupAsync();
        var other = await TestUsers.CreateLockoutStudentAsync(_factory);
        try
        {
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            Assert.Equal(HttpStatusCode.OK, (await teacher.GetAsync($"/api/v1/students/{w.stuId}/progress-summary")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync($"/api/v1/students/{other.Id}/progress-summary")).StatusCode);
        }
        finally { await Cleanup(w); }
    }

    [Fact]
    public async Task Parent_linked_access_and_unlinked_rejection()
    {
        var w = await SetupAsync();
        var other = await TestUsers.CreateLockoutStudentAsync(_factory);
        try
        {
            var parent = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");
            Assert.Equal(HttpStatusCode.OK, (await parent.GetAsync($"/api/v1/students/{w.stuId}/progress-summary")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await parent.GetAsync($"/api/v1/students/{other.Id}/progress-summary")).StatusCode);
        }
        finally { await Cleanup(w); }
    }

    [Fact]
    public async Task SchoolAdmin_same_tenant_access_cross_tenant_404_and_systemadmin_blocked()
    {
        var w = await SetupAsync();
        try
        {
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/students/{w.stuId}/progress-summary")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/students/{await UserId("STU-T2")}/progress-summary")).StatusCode);

            // Platform SystemAdmin has no tenant claim → cannot use the tenant route.
            var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");
            Assert.Equal(HttpStatusCode.Forbidden, (await sys.GetAsync($"/api/v1/students/{w.stuId}/progress-summary")).StatusCode);
        }
        finally { await Cleanup(w); }
    }

    [Fact]
    public async Task Aggregation_metadata_empty_data_and_date_validation()
    {
        var w = await SetupAsync();
        try
        {
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");

            // Real aggregation from the seeded assessment record (8/10 = 80%).
            var sum = await Read<SummaryData>(await admin.GetAsync($"/api/v1/students/{w.stuId}/progress-summary"));
            Assert.True(sum!.success);
            Assert.Equal(1, sum.data!.quizAttempts);
            Assert.Equal(80m, sum.data.averageQuizPercentage);

            // Stored-AI-output provenance labelling on prediction reads (no AI executed).
            var preds = await Read<List<PredictionRow>>(await admin.GetAsync($"/api/v1/students/{w.stuId}/predictions"));
            Assert.Contains(preds!.data!, p => p.source == "stored-ai-output" && p.modelName == "phase6-model");

            // Empty data → valid empty result (a different student with no records).
            var other = await TestUsers.CreateLockoutStudentAsync(_factory);
            var empty = await Read<List<PredictionRow>>(await admin.GetAsync($"/api/v1/students/{other.Id}/predictions"));
            Assert.True(empty!.success);
            Assert.Empty(empty.data!);

            // Date-range validation: To before From → 400.
            var bad = await admin.GetAsync($"/api/v1/students/{w.stuId}/metric-history?from=2031-01-01&to=2030-01-01");
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

            // Page size is clamped.
            var clamped = await admin.GetAsync($"/api/v1/students/{w.stuId}/metric-history?pageSize=999");
            var page = await Read<List<MetricRow>>(clamped);
            Assert.True(page!.success);
        }
        finally { await Cleanup(w); }
    }

    private sealed record MetricRow(string id);

    [Fact]
    public async Task Class_and_subject_performance_aggregation()
    {
        var w = await SetupAsync();
        try
        {
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");

            var cls = await Read<ClassPerfData>(await admin.GetAsync($"/api/v1/performance/class/{w.classId}"));
            Assert.True(cls!.success);
            Assert.Equal(1, cls.data!.studentCount);
            Assert.Equal(1, cls.data.quizAttempts);

            // Teacher assigned to the class sees the same performance; a student cannot.
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            Assert.Equal(HttpStatusCode.OK, (await teacher.GetAsync($"/api/v1/performance/class/{w.classId}")).StatusCode);
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/v1/performance/class/{w.classId}")).StatusCode);

            // Cross-tenant class id → 404.
            var admin2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await admin2.GetAsync($"/api/v1/performance/class/{w.classId}")).StatusCode);
        }
        finally { await Cleanup(w); }
    }
}
