using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// P1-10 privacy fix — students/parents must never receive unreviewed (Pending/Rejected)
/// pain points or staff-only AI internals (EvidenceSummary/ModelVersion/PromptVersion);
/// Teacher/SchoolAdmin retain the full diagnostic projection. Covers both read surfaces
/// that expose <c>PainPoint</c> data through the real HTTP pipeline against local
/// PostgreSQL: AiAnalysisController's GET .../history (AnalysisService.GetHistoryForCallerAsync)
/// and StudentProgressController's GET .../pain-points (StudentProgressService.PainPointsAsync).
/// </summary>
public class PainPointPrivacyTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public PainPointPrivacyTests(IntegrationFactory factory) => _factory = factory;

    private async Task<string> UserId(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    private sealed record World(string yearId, string classId, string enrollId, string tcaId, string psrId,
        string studentId, string studentLogin, string approvedId, string pendingId, string rejectedId, string predictionId);

    /// <summary>
    /// Seeds a throwaway tenant-1 student linked to TEACH-T1 (assigned teacher) and PARENT-T1
    /// (linked parent), with one Approved + one Pending + one Rejected pain point. Each pain
    /// point carries a unique Recommendation marker and Id so tests can prove exactly which
    /// items — and which fields — cross into each role's projection.
    /// </summary>
    private async Task<World> SetupAsync()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var teacherId = await UserId("TEACH-T1");
        var parentId = await UserId("PARENT-T1");

        var w = new World(Phase4Db.NewId("ay"), Phase4Db.NewId("cls"), Phase4Db.NewId("enr"), Phase4Db.NewId("tca"),
            Phase4Db.NewId("psr"), student.Id, student.LoginCode,
            Phase4Db.NewId("pp-appr"), Phase4Db.NewId("pp-pend"), Phase4Db.NewId("pp-rej"), Phase4Db.NewId("pred"));

        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.academicYears.Add(new AcademicYear { Id = w.yearId, TenantId = "tenant-1", Name = "Y", Code = Phase4Db.NewId("AY")[..12], StartDate = U(2030, 9, 1), EndDate = U(2031, 6, 30) });
        db.schoolClasses.Add(new SchoolClass { Id = w.classId, TenantId = "tenant-1", Name = "C", Code = Phase4Db.NewId("C")[..12], GradeId = "G7-ID", AcademicYearId = w.yearId, Capacity = 30 });
        db.enrollments.Add(new Enrollment { Id = w.enrollId, TenantId = "tenant-1", StudentId = w.studentId, SchoolClassId = w.classId, AcademicYearId = w.yearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
        db.teacherClassAssignments.Add(new TeacherClassAssignment { Id = w.tcaId, TenantId = "tenant-1", TeacherId = teacherId, SchoolClassId = w.classId, IsActive = true, ActiveFrom = DateTime.UtcNow });
        db.parentStudentRelationships.Add(new ParentStudentRelationship { Id = w.psrId, TenantId = "tenant-1", ParentId = parentId, StudentId = w.studentId, IsActive = true, CanViewProgress = true });

        db.painPoints.Add(new PainPoint
        {
            Id = w.approvedId, TenantId = "tenant-1", StudentId = w.studentId, Category = PainPointCategory.Concept,
            Title = "Fractions - approved", Description = "EVIDENCE-INTERNAL-APPROVED", Recommendation = "REC-APPROVED",
            ConfidenceScore = 0.7m, Escalation = EscalationLevel.Monitor, ReviewStatus = HumanReviewStatus.Approved,
            ModelVersion = "m-v1", PromptVersion = "analysis.v1", DetectedAt = DateTime.UtcNow,
        });
        db.painPoints.Add(new PainPoint
        {
            Id = w.pendingId, TenantId = "tenant-1", StudentId = w.studentId, Category = PainPointCategory.Skill,
            Title = "Fractions - pending", Description = "EVIDENCE-INTERNAL-PENDING", Recommendation = "REC-PENDING",
            ConfidenceScore = 0.6m, Escalation = EscalationLevel.None, ReviewStatus = HumanReviewStatus.Pending,
            ModelVersion = "m-v1", PromptVersion = "analysis.v1", DetectedAt = DateTime.UtcNow,
        });
        db.painPoints.Add(new PainPoint
        {
            Id = w.rejectedId, TenantId = "tenant-1", StudentId = w.studentId, Category = PainPointCategory.Engagement,
            Title = "Fractions - rejected", Description = "EVIDENCE-INTERNAL-REJECTED", Recommendation = "REC-REJECTED",
            ConfidenceScore = 0.5m, Escalation = EscalationLevel.Escalate, ReviewStatus = HumanReviewStatus.Rejected,
            ModelVersion = "m-v1", PromptVersion = "analysis.v1", DetectedAt = DateTime.UtcNow,
        });
        db.predictionRecords.Add(new PredictionRecord
        {
            Id = w.predictionId, TenantId = "tenant-1", StudentId = w.studentId, Kind = PredictionKind.Performance,
            PredictedScore = 0.82m, Level = PerformanceLevel.OnTrack, ConfidenceScore = 0.65m,
            ModelName = "MODEL-INTERNAL-NAME", ModelVersion = "MODEL-INTERNAL-VERSION", PredictedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return w;
    }

    private async Task CleanupAsync(World w)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var (table, id) in new[]
        {
            ("painPoints", w.approvedId), ("painPoints", w.pendingId), ("painPoints", w.rejectedId),
            ("predictionRecords", w.predictionId),
            ("parentStudentRelationships", w.psrId), ("teacherClassAssignments", w.tcaId),
            ("enrollments", w.enrollId), ("schoolClasses", w.classId), ("academicYears", w.yearId)
        })
            // Table name is a hardcoded constant from the literal array above (not user input).
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"Id\" = {{0}}", id);
#pragma warning restore EF1002
    }

    private static async Task<string> OkBodyAsync(HttpClient client, string url)
    {
        var resp = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return await resp.Content.ReadAsStringAsync();
    }

    private static void AssertSafeProjectionOnly(string body)
    {
        // Only the human-approved item is visible ...
        Assert.Contains("REC-APPROVED", body);
        Assert.DoesNotContain("REC-PENDING", body);
        Assert.DoesNotContain("REC-REJECTED", body);
        // ... and never with internal/staff-only evidence or AI provenance fields.
        Assert.DoesNotContain("evidenceSummary", body);
        Assert.DoesNotContain("modelVersion", body);
        Assert.DoesNotContain("promptVersion", body);
        Assert.DoesNotContain("escalationLevel", body);
        Assert.DoesNotContain("EVIDENCE-INTERNAL", body);
    }

    private static void AssertFullStaffProjection(string body)
    {
        Assert.Contains("REC-APPROVED", body);
        Assert.Contains("REC-PENDING", body);
        Assert.Contains("REC-REJECTED", body);
        Assert.Contains("evidenceSummary", body);
        Assert.Contains("modelVersion", body);
        Assert.Contains("promptVersion", body);
    }

    private static void AssertOnlyApprovedId(string body, World w)
    {
        Assert.Contains(w.approvedId, body);
        Assert.DoesNotContain(w.pendingId, body);
        Assert.DoesNotContain(w.rejectedId, body);
    }

    private static void AssertAllIds(string body, World w)
    {
        Assert.Contains(w.approvedId, body);
        Assert.Contains(w.pendingId, body);
        Assert.Contains(w.rejectedId, body);
    }

    // ---- /api/v1/ai/analysis/{studentId}/history (AnalysisService.GetHistoryForCallerAsync) ----

    [Fact]
    public async Task Student_and_parent_history_get_approved_only_safe_projection()
    {
        var w = await SetupAsync();
        try
        {
            var student = await TestClient.AuthedClientAsync(_factory, w.studentLogin);
            AssertSafeProjectionOnly(await OkBodyAsync(student, $"/api/v1/ai/analysis/{w.studentId}/history"));

            var parent = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");
            AssertSafeProjectionOnly(await OkBodyAsync(parent, $"/api/v1/ai/analysis/{w.studentId}/history"));
        }
        finally { await CleanupAsync(w); }
    }

    [Fact]
    public async Task Teacher_and_schooladmin_history_still_see_all_statuses_with_full_detail()
    {
        var w = await SetupAsync();
        try
        {
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            AssertFullStaffProjection(await OkBodyAsync(teacher, $"/api/v1/ai/analysis/{w.studentId}/history"));

            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            AssertFullStaffProjection(await OkBodyAsync(admin, $"/api/v1/ai/analysis/{w.studentId}/history"));
        }
        finally { await CleanupAsync(w); }
    }

    // ---- /api/v1/students/{studentId}/pain-points (StudentProgressService.PainPointsAsync) ----

    [Fact]
    public async Task Student_and_parent_painpoints_endpoint_filters_to_approved_only()
    {
        var w = await SetupAsync();
        try
        {
            var student = await TestClient.AuthedClientAsync(_factory, w.studentLogin);
            AssertOnlyApprovedId(await OkBodyAsync(student, $"/api/v1/students/{w.studentId}/pain-points"), w);

            var parent = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");
            AssertOnlyApprovedId(await OkBodyAsync(parent, $"/api/v1/students/{w.studentId}/pain-points"), w);
        }
        finally { await CleanupAsync(w); }
    }

    [Fact]
    public async Task Teacher_and_schooladmin_painpoints_endpoint_returns_all_statuses()
    {
        var w = await SetupAsync();
        try
        {
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            AssertAllIds(await OkBodyAsync(teacher, $"/api/v1/students/{w.studentId}/pain-points"), w);

            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            AssertAllIds(await OkBodyAsync(admin, $"/api/v1/students/{w.studentId}/pain-points"), w);
        }
        finally { await CleanupAsync(w); }
    }

    // ---- /api/v1/students/{studentId}/predictions (StudentProgressService.PredictionsAsync) ----
    // Same "model internals" category as PromptVersion/ModelVersion above (decision #7) — the
    // prediction OUTCOME is safe to show students/parents, the model name/version is not.

    [Fact]
    public async Task Student_and_parent_predictions_hide_model_internals()
    {
        var w = await SetupAsync();
        try
        {
            var student = await TestClient.AuthedClientAsync(_factory, w.studentLogin);
            var studentBody = await OkBodyAsync(student, $"/api/v1/students/{w.studentId}/predictions");
            Assert.Contains(w.predictionId, studentBody);
            Assert.DoesNotContain("MODEL-INTERNAL-NAME", studentBody);
            Assert.DoesNotContain("MODEL-INTERNAL-VERSION", studentBody);

            var parent = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");
            var parentBody = await OkBodyAsync(parent, $"/api/v1/students/{w.studentId}/predictions");
            Assert.Contains(w.predictionId, parentBody);
            Assert.DoesNotContain("MODEL-INTERNAL-NAME", parentBody);
            Assert.DoesNotContain("MODEL-INTERNAL-VERSION", parentBody);
        }
        finally { await CleanupAsync(w); }
    }

    [Fact]
    public async Task Teacher_and_schooladmin_predictions_still_include_model_internals()
    {
        var w = await SetupAsync();
        try
        {
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            var teacherBody = await OkBodyAsync(teacher, $"/api/v1/students/{w.studentId}/predictions");
            Assert.Contains("MODEL-INTERNAL-NAME", teacherBody);
            Assert.Contains("MODEL-INTERNAL-VERSION", teacherBody);

            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            var adminBody = await OkBodyAsync(admin, $"/api/v1/students/{w.studentId}/predictions");
            Assert.Contains("MODEL-INTERNAL-NAME", adminBody);
            Assert.Contains("MODEL-INTERNAL-VERSION", adminBody);
        }
        finally { await CleanupAsync(w); }
    }
}
