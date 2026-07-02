using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 6 §12 — performance-prediction orchestration through the real HTTP
/// pipeline against local PostgreSQL, AI client faked. Proves access rules,
/// authoritative feature derivation, insufficient-data handling, immutable
/// history persistence with model/feature-schema versions, AiUsage on success
/// and failure, and a stable upstream error with no feature-payload leakage.
/// </summary>
public class AiPredictionApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AiPredictionApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed record PredResult(string predictionId, string studentId, string predictionType, decimal score,
        string level, string riskBand, decimal? confidence, string modelName, string modelVersion,
        string featureSchemaVersion, string correlationId);

    private sealed class FakeAiClient : IAiRagClient
    {
        public Task<AiVisionAnalyzeResponse> AnalyzeVisionFrameAsync(AiVisionAnalyzeRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiVisionEndSessionResponse> EndVisionSessionAsync(AiVisionEndSessionRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public bool Throw;
        public Func<AiPredictionResponse>? On = null;
        public Task<AiPredictionResponse> PredictAsync(AiPredictionRequest r, string t, string? u, CancellationToken ct = default)
            => Throw ? throw new AiServiceException("provider_error", "AI failed")
                     : Task.FromResult(On?.Invoke() ?? Valid());

        public Task<AiTutorResponse> TutorAsync(AiTutorRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiIngestResponse> IngestDocumentAsync(AiIngestRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiDeleteResponse> DeleteDocumentAsync(string d, string c, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiQuizDraftResponse> QuizDraftAsync(AiQuizDraftRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiAnalysisResponse> AnalyzeAsync(AiAnalysisRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static AiPredictionResponse Valid() => new()
    {
        StudentRef = "s", PredictionType = "performance", Score = 78.5, Level = "Medium", RiskBand = "medium",
        Confidence = 0.82, ModelName = "rf-performance", ModelVersion = "rf-2026.06", FeatureSchemaVersion = "perf-v1",
        GeneratedAt = "now", Limitations = new() { "Advisory only — requires human review." },
    };

    private WebApplicationFactory<Program> WithFake(FakeAiClient fake) =>
        _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddScoped<IAiRagClient>(_ => fake)));

    private static async Task<HttpClient> AuthedAsync(WebApplicationFactory<Program> f, string code)
    {
        var c = f.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
        TestClient.LoginResponse? body = null;
        for (var i = 0; i < 4 && string.IsNullOrEmpty(body?.token); i++)
        {
            if (i > 0) await Task.Delay(150 * i);
            var r = await c.PostAsJsonAsync("/api/v1/account/login", new { UserID = code, Password = TestClient.Password });
            if (r.IsSuccessStatusCode) body = await r.Content.ReadFromJsonAsync<TestClient.LoginResponse>(Json);
        }
        c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body!.token);
        return c;
    }

    private async Task<string> UserId(string code)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == code)).Id;
    }

    private async Task SeedFeatureDataAsync(string studentId)
    {
        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        var user = await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.Id == studentId);
        user.Gender = Gender.Male;
        db.studentMetricHistories.Add(new StudentMetricHistory { Id = Phase4Db.NewId("m"), TenantId = "tenant-1", StudentId = studentId, MetricType = ProgressMetricType.Attendance, Value = 85m, MeasuredAt = DateTime.UtcNow.AddDays(-3) });
        db.studentMetricHistories.Add(new StudentMetricHistory { Id = Phase4Db.NewId("m"), TenantId = "tenant-1", StudentId = studentId, MetricType = ProgressMetricType.StudyTime, Value = 12m, MeasuredAt = DateTime.UtcNow.AddDays(-1) });
        db.studentLearningProfiles.Add(new StudentLearningProfile { Id = Phase4Db.NewId("slp"), TenantId = "tenant-1", StudentId = studentId, AgeYears = 14, SchoolType = "public", InternetAccess = "yes", TravelTime = "<15 min", ExtraActivities = "no", StudyMethod = "textbook", FeatureSchemaVersion = "perf-v1" });
        await db.SaveChangesAsync();
    }

    // Usage rows scoped to a unique caller id make assertions parallel-safe.
    private async Task<int> UsageCountForCaller(string callerId)
    {
        await using var db = Phase4Db.Platform(_factory);
        return await db.aiUsageRecords.IgnoreQueryFilters().CountAsync(u => u.Kind == AiUsageKind.Prediction && u.UserId == callerId);
    }

    private async Task<int> UsageCountForCorrelation(string correlationId)
    {
        await using var db = Phase4Db.Platform(_factory);
        return await db.aiUsageRecords.IgnoreQueryFilters().CountAsync(u => u.Kind == AiUsageKind.Prediction && u.CorrelationId == correlationId);
    }

    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    private sealed record RelWorld(string yearId, string classId, string enrollId, string tcaId, string psrId, string studentId, string studentLogin);

    /// <summary>
    /// Builds the real relationship graph (academic year → class → enrollment →
    /// teacher-class assignment + parent link) plus the authoritative feature data,
    /// through the established Phase4Db domain-seeding pattern. The REAL
    /// IStudentAccessAuthorizer is exercised end-to-end via the HTTP endpoint.
    /// </summary>
    private async Task<RelWorld> SetupRelWorldAsync(string teacherLogin, string parentLogin)
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var teacherId = await UserId(teacherLogin);
        var parentId = await UserId(parentLogin);
        var w = new RelWorld(Phase4Db.NewId("ay"), Phase4Db.NewId("cls"), Phase4Db.NewId("enr"),
            Phase4Db.NewId("tca"), Phase4Db.NewId("psr"), student.Id, student.LoginCode);

        await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
        {
            db.academicYears.Add(new AcademicYear { Id = w.yearId, TenantId = "tenant-1", Name = "Y", Code = Phase4Db.NewId("AY")[..12], StartDate = U(2030, 9, 1), EndDate = U(2031, 6, 30) });
            db.schoolClasses.Add(new SchoolClass { Id = w.classId, TenantId = "tenant-1", Name = "C", Code = Phase4Db.NewId("C")[..12], GradeId = "G7-ID", AcademicYearId = w.yearId, Capacity = 30 });
            db.enrollments.Add(new Enrollment { Id = w.enrollId, TenantId = "tenant-1", StudentId = student.Id, SchoolClassId = w.classId, AcademicYearId = w.yearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
            db.teacherClassAssignments.Add(new TeacherClassAssignment { Id = w.tcaId, TenantId = "tenant-1", TeacherId = teacherId, SchoolClassId = w.classId, IsActive = true, ActiveFrom = DateTime.UtcNow });
            db.parentStudentRelationships.Add(new ParentStudentRelationship { Id = w.psrId, TenantId = "tenant-1", ParentId = parentId, StudentId = student.Id, IsActive = true, CanViewProgress = true });
            await db.SaveChangesAsync();
        }
        await SeedFeatureDataAsync(student.Id);
        return w;
    }

    private async Task CleanupRelWorldAsync(RelWorld w)
    {
        await using var db = Phase4Db.Platform(_factory);
        db.predictionRecords.RemoveRange(await db.predictionRecords.IgnoreQueryFilters().Where(p => p.StudentId == w.studentId).ToListAsync());
        db.studentMetricHistories.RemoveRange(await db.studentMetricHistories.IgnoreQueryFilters().Where(m => m.StudentId == w.studentId).ToListAsync());
        db.studentLearningProfiles.RemoveRange(await db.studentLearningProfiles.IgnoreQueryFilters().Where(p => p.StudentId == w.studentId).ToListAsync());
        foreach (var (table, id) in new[]
        {
            ("parentStudentRelationships", w.psrId), ("teacherClassAssignments", w.tcaId),
            ("enrollments", w.enrollId), ("schoolClasses", w.classId), ("academicYears", w.yearId)
        })
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"Id\" = {{0}}", id);
#pragma warning restore EF1002
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Assigned_teacher_can_predict_and_retrieve()
    {
        var w = await SetupRelWorldAsync("TEACH-T1", "PARENT-T1");
        try
        {
            var teacher = await AuthedAsync(WithFake(new FakeAiClient()), "TEACH-T1");

            var resp = await teacher.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = w.studentId });
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var result = await resp.Content.ReadFromJsonAsync<PredResult>(Json);
            Assert.Equal(w.studentId, result!.studentId);           // correct target
            Assert.Equal("Medium", result.level);

            await using (var db = Phase4Db.Platform(_factory))
            {
                var records = await db.predictionRecords.IgnoreQueryFilters().Where(p => p.StudentId == w.studentId).ToListAsync();
                Assert.Single(records);                              // PredictionRecord persisted
                Assert.Equal("tenant-1", records[0].TenantId);       // tenant scope preserved
            }
            Assert.Equal(1, await UsageCountForCorrelation(result.correlationId)); // AiUsage persisted

            var hist = await teacher.GetAsync($"/api/v1/ai/prediction/{w.studentId}/history");
            Assert.Equal(HttpStatusCode.OK, hist.StatusCode);
            var items = await hist.Content.ReadFromJsonAsync<List<PredictionHistoryItemDto>>(Json);
            Assert.Single(items!);                                   // only this student's record (no unrelated data)
        }
        finally { await CleanupRelWorldAsync(w); }
    }

    [Fact]
    public async Task Linked_parent_can_predict_and_retrieve()
    {
        var w = await SetupRelWorldAsync("TEACH-T1", "PARENT-T1");
        try
        {
            var parent = await AuthedAsync(WithFake(new FakeAiClient()), "PARENT-T1");

            var resp = await parent.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = w.studentId });
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var result = await resp.Content.ReadFromJsonAsync<PredResult>(Json);
            Assert.Equal(w.studentId, result!.studentId);

            await using (var db = Phase4Db.Platform(_factory))
            {
                var records = await db.predictionRecords.IgnoreQueryFilters().Where(p => p.StudentId == w.studentId).ToListAsync();
                Assert.Single(records);
                Assert.Equal("tenant-1", records[0].TenantId);
            }
            Assert.Equal(1, await UsageCountForCorrelation(result.correlationId));

            var hist = await parent.GetAsync($"/api/v1/ai/prediction/{w.studentId}/history");
            Assert.Equal(HttpStatusCode.OK, hist.StatusCode);
            var items = await hist.Content.ReadFromJsonAsync<List<PredictionHistoryItemDto>>(Json);
            Assert.Single(items!);
        }
        finally { await CleanupRelWorldAsync(w); }
    }

    [Fact]
    public async Task Insufficient_data_is_rejected()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory); // fresh student, no feature data
        var admin = await AuthedAsync(WithFake(new FakeAiClient()), "ADMIN-T1");
        var resp = await admin.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = student.Id });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Admin_generates_persists_history_with_versions_and_usage()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var adminUser = await TestUsers.CreateSchoolAdminAsync(_factory);
        await SeedFeatureDataAsync(student.Id);

        var admin = await AuthedAsync(WithFake(new FakeAiClient()), adminUser.LoginCode);

        var r1 = await admin.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = student.Id });
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var result = await r1.Content.ReadFromJsonAsync<PredResult>(Json);
        Assert.Equal("Medium", result!.level);
        Assert.Equal("rf-2026.06", result.modelVersion);
        Assert.Equal("perf-v1", result.featureSchemaVersion);

        // second prediction -> a NEW historical record (no overwrite)
        var r2 = await admin.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = student.Id });
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        await using var db2 = Phase4Db.Platform(_factory);
        var records = await db2.predictionRecords.IgnoreQueryFilters().Where(p => p.StudentId == student.Id).ToListAsync();
        Assert.Equal(2, records.Count);                       // history preserved, not overwritten
        Assert.All(records, p => Assert.Equal(PredictionKind.Performance, p.Kind));
        Assert.All(records, p => Assert.Equal("rf-2026.06", p.ModelVersion));
        Assert.All(records, p => Assert.True(p.ConfidenceScore > 0));

        Assert.Equal(2, await UsageCountForCaller(adminUser.Id));   // AiUsage recorded per call (scoped to caller)

        var hist = await admin.GetAsync($"/api/v1/ai/prediction/{student.Id}/history");
        Assert.Equal(HttpStatusCode.OK, hist.StatusCode);
        var items = await hist.Content.ReadFromJsonAsync<List<PredictionHistoryItemDto>>(Json);
        Assert.Equal(2, items!.Count);
    }

    [Fact]
    public async Task Student_self_allowed_other_student_forbidden()
    {
        var self = await TestUsers.CreateLockoutStudentAsync(_factory);
        var other = await UserId("ST-001");
        await SeedFeatureDataAsync(self.Id);
        var stu = await AuthedAsync(WithFake(new FakeAiClient()), self.LoginCode);

        var ok = await stu.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = self.Id });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var forbidden = await stu.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = other });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    /// <summary>
    /// Route/RBAC audit §8.1 gap 3 — POST /api/v1/ai/prediction has no role gate (a Student may
    /// call it), which the audit flagged as worth investigating. Decision: this is confirmed
    /// INTENTIONAL self/relationship service (see AiPredictionApiTests class remarks and the
    /// final RBAC-audit report) — the sibling analysis-generation endpoint
    /// (AiAnalysisController.Generate) is deliberately staff-only, but prediction generation is a
    /// numeric ML read over already-stored objective metrics, consistently open to self/assigned-
    /// teacher/linked-parent/same-tenant-admin via IStudentAccessAuthorizer, exactly like every
    /// other test in this class already proves. Ownership — not role — is the gate, and it is
    /// enforced BEFORE the tenant AI-quota check (PredictionService.GenerateAsync calls
    /// EnsureCanAccessStudentAsync prior to EnsureWithinAiMonthlyQuotaAsync), so this assertion is
    /// deliberately independent of feature-data seeding and of the tenant's current AI-usage quota.
    /// </summary>
    [Fact]
    public async Task Role_is_permissive_by_design_but_ownership_still_blocks_student_targeting_a_different_student()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var other = await UserId("ST-001");
        var stu = await AuthedAsync(WithFake(new FakeAiClient()), student.LoginCode);

        var resp = await stu.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = other });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Cross_tenant_target_returns_404()
    {
        var t2Student = await UserId("STU-T2");
        var admin = await AuthedAsync(WithFake(new FakeAiClient()), "ADMIN-T1");
        var resp = await admin.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = t2Student });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Unassigned_teacher_forbidden()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var teacher = await AuthedAsync(WithFake(new FakeAiClient()), "TEACH-T1");
        var resp = await teacher.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = student.Id });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Unlinked_parent_forbidden()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory); // no parent link
        var parent = await AuthedAsync(WithFake(new FakeAiClient()), "PARENT-T1");
        var resp = await parent.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = student.Id });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Provider_failure_returns_502_records_failed_usage_and_no_payload_leak()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var adminUser = await TestUsers.CreateSchoolAdminAsync(_factory);
        await SeedFeatureDataAsync(student.Id);

        var admin = await AuthedAsync(WithFake(new FakeAiClient { Throw = true }), adminUser.LoginCode);
        var resp = await admin.PostAsJsonAsync("/api/v1/ai/prediction", new { studentId = student.Id });
        Assert.Equal(HttpStatusCode.BadGateway, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("textbook", body);            // no feature payload leakage
        Assert.DoesNotContain("attendance", body.ToLowerInvariant());

        Assert.Equal(1, await UsageCountForCaller(adminUser.Id));   // failure usage recorded (scoped to caller)
    }
}
