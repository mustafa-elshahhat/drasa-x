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
/// Phase 6 §11 — AI quiz-draft persistence through the real HTTP pipeline against
/// local PostgreSQL, with the AI client faked. Proves: Draft/Origin=AiGenerated
/// persistence with questions+options+correct answers, QuizGeneration provenance,
/// AiUsage on success and failure, teacher-assignment authorization, no
/// auto-publish, and a stable upstream error on provider failure.
/// </summary>
public class AiQuizDraftApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AiQuizDraftApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed record DraftResult(string quizId, string quizGenerationId, string status, string origin,
        string title, int questionCount, bool grounded, int citationCount, string provider, string model,
        string promptVersion, string correlationId);

    private sealed class FakeAiClient : IAiRagClient
    {
        public Task<AiVisionAnalyzeResponse> AnalyzeVisionFrameAsync(AiVisionAnalyzeRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiVisionEndSessionResponse> EndVisionSessionAsync(AiVisionEndSessionRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public bool Throw;
        public AiQuizDraftResponse Response = ValidDraft();

        public Task<AiQuizDraftResponse> QuizDraftAsync(AiQuizDraftRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
            => Throw ? throw new AiServiceException("provider_error", "AI failed") : Task.FromResult(Response);

        public Task<AiTutorResponse> TutorAsync(AiTutorRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiIngestResponse> IngestDocumentAsync(AiIngestRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiDeleteResponse> DeleteDocumentAsync(string d, string c, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiPredictionResponse> PredictAsync(AiPredictionRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiAnalysisResponse> AnalyzeAsync(AiAnalysisRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static AiQuizDraftResponse ValidDraft() => new()
    {
        Grounded = true, Provider = "groq", Model = "llama-3.1-8b-instant", ModelVersion = "llama-3.1-8b-instant",
        PromptVersion = "quiz.v1", RetrievalCount = 2, CorrelationId = "ai-corr", GeneratedAt = "now",
        Citations = new() { new AiTutorCitation { SourceDocumentId = "doc-1", ChunkId = "c", Score = 0.9 } },
        Draft = new AiQuizDraft
        {
            Title = "Photosynthesis Draft", Instructions = "Answer all.", Grade = 8, Subject = "Science",
            Difficulty = "core", QuestionCount = 2,
            Questions = new()
            {
                new AiQuizDraftQuestion { QuestionType = "mcq", QuestionText = "What does chlorophyll absorb?",
                    Options = new() { "Sunlight", "Water", "Soil", "Air" }, CorrectIndex = 0, Explanation = "from text", Points = 2,
                    SourceReferences = new() { "doc-1" } },
                new AiQuizDraftQuestion { QuestionType = "true_false", QuestionText = "Plants release oxygen.",
                    Options = new() { "True", "False" }, CorrectIndex = 0, Explanation = "by-product", Points = 1,
                    SourceReferences = new() { "doc-1" } },
            }
        }
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

    private async Task<string> SeedSubjectAsync()
    {
        var subjectId = Phase4Db.NewId("subj");
        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.subjects.Add(new Subject { Id = subjectId, TenantId = "tenant-1", Name = "Science", GradeId = "G7-ID" });
        await db.SaveChangesAsync();
        return subjectId;
    }

    private async Task<string> UserIdAsync(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    // A subject WITH an active TeacherSubjectAssignment for `teacherCode` — needed because
    // AiQuizController is Teacher-only (SchoolAdmin Teacher-portal removal): a Teacher actor
    // must be genuinely assigned to the subject to pass QuizDraftService's authorization,
    // unlike the SchoolAdmin tenant-wide bypass this suite previously relied on.
    private async Task<(string subjectId, string assignmentId)> SeedSubjectAssignedToAsync(string teacherCode)
    {
        var subjectId = await SeedSubjectAsync();
        var teacherId = await UserIdAsync(teacherCode);
        var assignmentId = Phase4Db.NewId("tsa");
        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.teacherSubjectAssignments.Add(new TeacherSubjectAssignment
        {
            Id = assignmentId, TenantId = "tenant-1", TeacherId = teacherId, SubjectId = subjectId, ActiveFrom = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (subjectId, assignmentId);
    }

    private async Task CleanupQuizAsync(string subjectId, string? quizId, string? assignmentId = null)
    {
        await using var db = Phase4Db.Platform(_factory);
        if (quizId is not null)
        {
            var qIds = await db.questions.IgnoreQueryFilters().Where(q => q.QuizId == quizId).Select(q => q.Id).ToListAsync();
            db.questionOptions.RemoveRange(await db.questionOptions.IgnoreQueryFilters().Where(o => qIds.Contains(o.QuestionId)).ToListAsync());
            db.questions.RemoveRange(await db.questions.IgnoreQueryFilters().Where(q => q.QuizId == quizId).ToListAsync());
            db.quizGenerations.RemoveRange(await db.quizGenerations.IgnoreQueryFilters().Where(g => g.QuizId == quizId).ToListAsync());
            db.quizzes.RemoveRange(await db.quizzes.IgnoreQueryFilters().Where(q => q.Id == quizId).ToListAsync());
        }
        if (assignmentId is not null)
            db.teacherSubjectAssignments.RemoveRange(await db.teacherSubjectAssignments.IgnoreQueryFilters().Where(a => a.Id == assignmentId).ToListAsync());
        db.subjects.RemoveRange(await db.subjects.IgnoreQueryFilters().Where(s => s.Id == subjectId).ToListAsync());
        await db.SaveChangesAsync();
    }

    // SchoolAdmin Teacher-portal removal: AiQuizController is now Teacher-only, so the primary
    // actor here is an assigned Teacher, not SchoolAdmin (which previously relied on a
    // tenant-wide bypass of the subject-assignment check — see
    // SchoolAdmin_generate_draft_is_forbidden_403 for the new negative case).
    [Fact]
    public async Task Teacher_generates_draft_persisted_as_Draft_Origin_AI_with_provenance_and_usage()
    {
        var (subjectId, assignmentId) = await SeedSubjectAssignedToAsync("TEACH-T1");
        string? quizId = null;
        try
        {
            var f = WithFake(new FakeAiClient());
            var teacher = await AuthedAsync(f, "TEACH-T1");

            var resp = await teacher.PostAsJsonAsync("/api/v1/ai/quiz/draft", new
            {
                subjectId, numQuestions = 2, difficulty = "core", language = "en",
                questionTypes = new[] { "mcq", "true_false" }, topic = "photosynthesis"
            });
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var result = await resp.Content.ReadFromJsonAsync<DraftResult>(Json);
            quizId = result!.quizId;

            Assert.Equal("Draft", result.status);
            Assert.Equal("AiGenerated", result.origin);
            Assert.Equal(2, result.questionCount);
            Assert.True(result.grounded);
            Assert.Equal("quiz.v1", result.promptVersion);

            await using var db = Phase4Db.Platform(_factory);
            var quiz = await db.quizzes.IgnoreQueryFilters().FirstAsync(q => q.Id == quizId);
            Assert.Equal(QuizStatus.Draft, quiz.Status);            // not auto-published
            Assert.Equal(QuizOrigin.AiGenerated, quiz.Origin);
            Assert.Equal("tenant-1", quiz.TenantId);

            var qs = await db.questions.IgnoreQueryFilters().Where(q => q.QuizId == quizId).ToListAsync();
            Assert.Equal(2, qs.Count);
            var opts = await db.questionOptions.IgnoreQueryFilters()
                .Where(o => qs.Select(x => x.Id).Contains(o.QuestionId)).ToListAsync();
            Assert.Contains(opts, o => o.IsCorrect);                // correct answer persisted (securely on option)
            Assert.True(opts.Count(o => o.IsCorrect) == 2);         // exactly one correct per question

            var gen = await db.quizGenerations.IgnoreQueryFilters().FirstAsync(g => g.QuizId == quizId);
            Assert.Equal(QuizGenerationStatus.Pending, gen.Status); // awaiting teacher review
            Assert.Equal("quiz.v1", gen.PromptVersion);
            Assert.Equal("groq", gen.AiProvider);

            var usage = await db.aiUsageRecords.IgnoreQueryFilters()
                .Where(u => u.CorrelationId == result.correlationId && u.Kind == AiUsageKind.QuizGeneration).ToListAsync();
            Assert.Single(usage);
        }
        finally { await CleanupQuizAsync(subjectId, quizId, assignmentId); }
    }

    [Fact]
    public async Task Teacher_not_assigned_to_subject_is_forbidden()
    {
        var subjectId = await SeedSubjectAsync();
        try
        {
            var f = WithFake(new FakeAiClient());
            var teacher = await AuthedAsync(f, "TEACH-T1");
            var resp = await teacher.PostAsJsonAsync("/api/v1/ai/quiz/draft", new
            {
                subjectId, numQuestions = 2, difficulty = "core", questionTypes = new[] { "mcq" }
            });
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }
        finally { await CleanupQuizAsync(subjectId, null); }
    }

    [Fact]
    public async Task Provider_failure_returns_502_and_records_failed_usage()
    {
        var (subjectId, assignmentId) = await SeedSubjectAssignedToAsync("TEACH-T1");
        try
        {
            int before;
            await using (var db = Phase4Db.Platform(_factory))
                before = await db.aiUsageRecords.IgnoreQueryFilters().CountAsync(u => u.Kind == AiUsageKind.QuizGeneration);

            var f = WithFake(new FakeAiClient { Throw = true });
            var teacher = await AuthedAsync(f, "TEACH-T1");
            var resp = await teacher.PostAsJsonAsync("/api/v1/ai/quiz/draft", new
            {
                subjectId, numQuestions = 2, difficulty = "core", questionTypes = new[] { "mcq" }
            });
            Assert.Equal(HttpStatusCode.BadGateway, resp.StatusCode);   // AI_UNAVAILABLE

            await using var db2 = Phase4Db.Platform(_factory);
            var after = await db2.aiUsageRecords.IgnoreQueryFilters().CountAsync(u => u.Kind == AiUsageKind.QuizGeneration);
            Assert.True(after >= before + 1);                            // failure usage recorded; other AI tests may run in parallel
        }
        finally { await CleanupQuizAsync(subjectId, null, assignmentId); }
    }

    // SchoolAdmin Teacher-portal removal: AiQuizController no longer admits SchoolAdmin at all
    // (previously TeacherOrSchoolAdmin with a tenant-wide bypass of the assignment check).
    [Fact]
    public async Task SchoolAdmin_generate_draft_is_forbidden_403()
    {
        var subjectId = await SeedSubjectAsync();
        try
        {
            var f = WithFake(new FakeAiClient());
            var admin = await AuthedAsync(f, "ADMIN-T1");
            var resp = await admin.PostAsJsonAsync("/api/v1/ai/quiz/draft", new
            {
                subjectId, numQuestions = 2, difficulty = "core", questionTypes = new[] { "mcq" }
            });
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }
        finally { await CleanupQuizAsync(subjectId, null); }
    }
}
