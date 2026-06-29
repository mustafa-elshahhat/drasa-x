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
/// Phase 6 §13 — conversation / pain-point analysis through the real HTTP
/// pipeline against local PostgreSQL, AI client faked. Proves teacher/admin-only
/// generation, the real access authorizer, PainPoint persistence with mandatory
/// Pending review + model/prompt versions, AiUsage success/failure, teacher
/// review, the parent-safe approved-only projection, and stable errors.
/// </summary>
public class AiAnalysisApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AiAnalysisApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed record AnalysisResult(string painPointId, string studentId, string category, string reviewStatus,
        string modelVersion, string promptVersion, string correlationId);

    private sealed class FakeAiClient : IAiRagClient
    {
        public Task<AiVisionAnalyzeResponse> AnalyzeVisionFrameAsync(AiVisionAnalyzeRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiVisionEndSessionResponse> EndVisionSessionAsync(AiVisionEndSessionRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public bool Throw;
        public Task<AiAnalysisResponse> AnalyzeAsync(AiAnalysisRequest r, string t, string? u, CancellationToken ct = default)
            => Throw ? throw new AiServiceException("provider_error", "AI failed")
                     : Task.FromResult(new AiAnalysisResponse
                     {
                         StudentRef = r.StudentRef, PainPointCategory = "concept", EvidenceSummary = "struggles with fraction steps",
                         Recommendation = "practice step-by-step fractions", Confidence = 0.7, EscalationLevel = "monitor",
                         HumanReviewRequired = true, Model = "m", ModelVersion = "m-v1", PromptVersion = "analysis.v1",
                         GeneratedAt = "now", CorrelationId = r.CorrelationId,
                     });

        public Task<AiTutorResponse> TutorAsync(AiTutorRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiIngestResponse> IngestDocumentAsync(AiIngestRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiDeleteResponse> DeleteDocumentAsync(string d, string c, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiQuizDraftResponse> QuizDraftAsync(AiQuizDraftRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiPredictionResponse> PredictAsync(AiPredictionRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
    }

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

    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);
    private sealed record RelWorld(string yearId, string classId, string enrollId, string tcaId, string psrId, string studentId);

    private async Task<RelWorld> SetupRelWorldAsync()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var teacherId = await UserId("TEACH-T1");
        var parentId = await UserId("PARENT-T1");
        var w = new RelWorld(Phase4Db.NewId("ay"), Phase4Db.NewId("cls"), Phase4Db.NewId("enr"),
            Phase4Db.NewId("tca"), Phase4Db.NewId("psr"), student.Id);
        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.academicYears.Add(new AcademicYear { Id = w.yearId, TenantId = "tenant-1", Name = "Y", Code = Phase4Db.NewId("AY")[..12], StartDate = U(2030, 9, 1), EndDate = U(2031, 6, 30) });
        db.schoolClasses.Add(new SchoolClass { Id = w.classId, TenantId = "tenant-1", Name = "C", Code = Phase4Db.NewId("C")[..12], GradeId = "G7-ID", AcademicYearId = w.yearId, Capacity = 30 });
        db.enrollments.Add(new Enrollment { Id = w.enrollId, TenantId = "tenant-1", StudentId = student.Id, SchoolClassId = w.classId, AcademicYearId = w.yearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
        db.teacherClassAssignments.Add(new TeacherClassAssignment { Id = w.tcaId, TenantId = "tenant-1", TeacherId = teacherId, SchoolClassId = w.classId, IsActive = true, ActiveFrom = DateTime.UtcNow });
        db.parentStudentRelationships.Add(new ParentStudentRelationship { Id = w.psrId, TenantId = "tenant-1", ParentId = parentId, StudentId = student.Id, IsActive = true, CanViewProgress = true });
        await db.SaveChangesAsync();
        return w;
    }

    private async Task CleanupAsync(RelWorld w)
    {
        await using var db = Phase4Db.Platform(_factory);
        db.painPoints.RemoveRange(await db.painPoints.IgnoreQueryFilters().Where(p => p.StudentId == w.studentId).ToListAsync());
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

    private static object ConvBody(string studentId) => new
    {
        studentId,
        conversation = new[] { new { role = "user", content = "I keep getting fractions wrong, I don't understand the steps." } },
        subject = "Math", language = "en",
    };

    private async Task<int> UsageCountForCorrelation(string correlationId)
    {
        await using var db = Phase4Db.Platform(_factory);
        return await db.aiUsageRecords.IgnoreQueryFilters().CountAsync(u => u.Kind == AiUsageKind.Recommendation && u.CorrelationId == correlationId);
    }

    [Fact]
    public async Task Assigned_teacher_generates_pending_painpoint_with_versions_and_usage()
    {
        var w = await SetupRelWorldAsync();
        try
        {
            var teacher = await AuthedAsync(WithFake(new FakeAiClient()), "TEACH-T1");
            var resp = await teacher.PostAsJsonAsync("/api/v1/ai/analysis", ConvBody(w.studentId));
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var result = await resp.Content.ReadFromJsonAsync<AnalysisResult>(Json);
            Assert.Equal("Concept", result!.category);
            Assert.Equal("Pending", result.reviewStatus);
            Assert.Equal("analysis.v1", result.promptVersion);

            await using var db = Phase4Db.Platform(_factory);
            var pp = await db.painPoints.IgnoreQueryFilters().FirstAsync(p => p.Id == result.painPointId);
            Assert.Equal(HumanReviewStatus.Pending, pp.ReviewStatus);     // mandatory human review
            Assert.Equal(EscalationLevel.Monitor, pp.Escalation);
            Assert.Equal("m-v1", pp.ModelVersion);
            Assert.Equal("analysis.v1", pp.PromptVersion);
            Assert.Equal("tenant-1", pp.TenantId);
            Assert.Equal(1, await UsageCountForCorrelation(result.correlationId));
        }
        finally { await CleanupAsync(w); }
    }

    [Fact]
    public async Task Parent_cannot_generate_analysis()
    {
        var w = await SetupRelWorldAsync();
        try
        {
            var parent = await AuthedAsync(WithFake(new FakeAiClient()), "PARENT-T1");
            var resp = await parent.PostAsJsonAsync("/api/v1/ai/analysis", ConvBody(w.studentId));
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);   // internal analysis is staff-only
        }
        finally { await CleanupAsync(w); }
    }

    [Fact]
    public async Task Teacher_review_then_parent_sees_safe_projection_only()
    {
        var w = await SetupRelWorldAsync();
        try
        {
            var teacher = await AuthedAsync(WithFake(new FakeAiClient()), "TEACH-T1");
            var gen = await teacher.PostAsJsonAsync("/api/v1/ai/analysis", ConvBody(w.studentId));
            var result = await gen.Content.ReadFromJsonAsync<AnalysisResult>(Json);

            // before approval, parent sees nothing (approved-only projection)
            var parent = await AuthedAsync(WithFake(new FakeAiClient()), "PARENT-T1");
            var preItems = await (await parent.GetAsync($"/api/v1/ai/analysis/{w.studentId}/history")).Content.ReadAsStringAsync();
            Assert.DoesNotContain(result!.painPointId, preItems);

            // teacher approves
            var review = await teacher.PutAsJsonAsync($"/api/v1/ai/analysis/{result.painPointId}/review", new { decision = "approve" });
            Assert.Equal(HttpStatusCode.NoContent, review.StatusCode);

            // teacher history = full (has internal evidence)
            var teacherHist = await (await teacher.GetAsync($"/api/v1/ai/analysis/{w.studentId}/history")).Content.ReadAsStringAsync();
            Assert.Contains("evidenceSummary", teacherHist);
            Assert.Contains("Approved", teacherHist);

            // parent history = safe projection (category + recommendation, NO internal evidence/escalation/model)
            var parentHist = await (await parent.GetAsync($"/api/v1/ai/analysis/{w.studentId}/history")).Content.ReadAsStringAsync();
            Assert.Contains("category", parentHist);
            Assert.DoesNotContain("evidenceSummary", parentHist);
            Assert.DoesNotContain("escalationLevel", parentHist);
            Assert.DoesNotContain("modelVersion", parentHist);
        }
        finally { await CleanupAsync(w); }
    }

    [Fact]
    public async Task Cross_tenant_student_returns_404()
    {
        var t2 = await UserId("STU-T2");
        var admin = await AuthedAsync(WithFake(new FakeAiClient()), "ADMIN-T1");
        var resp = await admin.PostAsJsonAsync("/api/v1/ai/analysis", ConvBody(t2));
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Provider_failure_returns_502_and_records_failed_usage()
    {
        var w = await SetupRelWorldAsync();
        var adminUser = await TestUsers.CreateSchoolAdminAsync(_factory);
        try
        {
            int before;
            await using (var db = Phase4Db.Platform(_factory))
                before = await db.aiUsageRecords.IgnoreQueryFilters().CountAsync(u => u.Kind == AiUsageKind.Recommendation && u.UserId == adminUser.Id);

            var admin = await AuthedAsync(WithFake(new FakeAiClient { Throw = true }), adminUser.LoginCode);
            var resp = await admin.PostAsJsonAsync("/api/v1/ai/analysis", ConvBody(w.studentId));
            Assert.Equal(HttpStatusCode.BadGateway, resp.StatusCode);

            await using var db2 = Phase4Db.Platform(_factory);
            var after = await db2.aiUsageRecords.IgnoreQueryFilters().CountAsync(u => u.Kind == AiUsageKind.Recommendation && u.UserId == adminUser.Id);
            Assert.Equal(before + 1, after);
        }
        finally { await CleanupAsync(w); }
    }
}
