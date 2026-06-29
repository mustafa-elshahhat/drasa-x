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
using DerasaX.Domain.Exceptions;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 15 — computer-vision attendance + engagement. Exercises the full backend
/// flow through a FAKE IAiRagClient (deterministic, no AI service needed): session
/// lifecycle, role authorization, tenant isolation, AI-unavailable handling, frame
/// persistence, candidate creation, confirm/reject/override, idempotent attendance,
/// audit, enrollment-based identity mapping, and student/parent read-only scoping.
/// </summary>
public class Phase15ComputerVisionApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase15ComputerVisionApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // A tiny valid 1x1 PNG (the fake AI ignores pixels; the backend only checks size).
    private const string TinyPng =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    // ---------------- fake AI client ----------------
    private sealed class FakeVisionAi : IAiRagClient
    {
        public bool ThrowUnavailable;
        public Func<AiVisionAnalyzeRequest, AiVisionAnalyzeResponse>? OnAnalyze;

        public Task<AiVisionAnalyzeResponse> AnalyzeVisionFrameAsync(AiVisionAnalyzeRequest r, string t, string? u, CancellationToken ct = default)
            => ThrowUnavailable
                ? throw new AiServiceException("unavailable", "AI service is unavailable.")
                : Task.FromResult(OnAnalyze?.Invoke(r) ?? TwoFaces(r));

        public Task<AiVisionEndSessionResponse> EndVisionSessionAsync(AiVisionEndSessionRequest r, string t, string? u, CancellationToken ct = default)
            => Task.FromResult(new AiVisionEndSessionResponse { SessionId = r.SessionId, BuffersCleared = 1 });

        public Task<AiTutorResponse> TutorAsync(AiTutorRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiIngestResponse> IngestDocumentAsync(AiIngestRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiDeleteResponse> DeleteDocumentAsync(string d, string c, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiQuizDraftResponse> QuizDraftAsync(AiQuizDraftRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiPredictionResponse> PredictAsync(AiPredictionRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiAnalysisResponse> AnalyzeAsync(AiAnalysisRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static AiVisionAnalyzeResponse TwoFaces(AiVisionAnalyzeRequest r, string engagement = "NotReady") => new()
    {
        CorrelationId = r.CorrelationId,
        SessionId = r.SessionId,
        FacesDetected = 2,
        Engine = "stub",
        Degraded = true,
        ModelVersion = "stub-cv-2026.06",
        SequenceLength = 16,
        GeneratedAt = "2026-06-28T00:00:00Z",
        QualityFlags = new() { "degraded_stub_engine" },
        Results = new()
        {
            new AiVisionFaceResult { TrackId = "ext-AAA", ExternalLabelId = "ext-AAA", Bbox = new() { 0, 0, 10, 10 },
                RecognitionConfidence = 0.80, RecognitionStatus = "candidate", Emotion = "Happy", EmotionConfidence = 0.7,
                Engagement = engagement, EngagementConfidence = engagement == "NotReady" ? 0.0 : 0.9, EngagementFrames = engagement == "NotReady" ? 1 : 16, EngagementFramesRequired = 16 },
            new AiVisionFaceResult { TrackId = "ext-BBB", ExternalLabelId = "ext-BBB", Bbox = new() { 20, 20, 30, 30 },
                RecognitionConfidence = 0.20, RecognitionStatus = "low_confidence", Emotion = "Sad", EmotionConfidence = 0.6,
                Engagement = "NotReady", EngagementConfidence = 0.0, EngagementFrames = 1, EngagementFramesRequired = 16 },
        }
    };

    // ---------------- helpers ----------------
    private WebApplicationFactory<Program> WithFake(FakeVisionAi fake) =>
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

    private async Task<string> StudentIdAsync(string code)
    {
        var (_, body) = await TestClient.LoginAsync(TestClient.NewClient(_factory), code);
        return body!.id!;
    }

    private static async Task<JsonElement> DataAsync(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    private static async Task<string> StartSessionAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/vision/sessions", new { title = "CV " + Guid.NewGuid().ToString("N")[..6] });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await DataAsync(resp)).GetProperty("id").GetString()!;
    }

    private static Task<HttpResponseMessage> AnalyzeAsync(HttpClient client, string sessionId, int frame = 0) =>
        client.PostAsJsonAsync($"/api/v1/vision/sessions/{sessionId}/analyze", new { imageBase64 = TinyPng, frameIndex = frame, wantEngagement = true });

    private async Task<List<JsonElement>> CandidatesAsync(HttpClient client, string sessionId)
    {
        var resp = await client.GetAsync($"/api/v1/vision/sessions/{sessionId}/candidates");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await DataAsync(resp)).EnumerateArray().ToList();
    }

    private async Task<(string code, string id)> CreateStaffAsync(string tenantId, string role)
    {
        var code = role[..4].ToUpperInvariant() + "-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
        ApplicationUser user = role == "SchoolAdmin"
            ? new SchoolAdmin { UserName = code.ToLowerInvariant(), FullName = "X", LoginCode = code, TenantId = tenantId }
            : new Teacher { UserName = code.ToLowerInvariant(), FullName = "X", LoginCode = code, TenantId = tenantId };
        var res = await users.CreateAsync(user, TestClient.Password);
        Assert.True(res.Succeeded, string.Join(",", res.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);
        return (code, user.Id);
    }

    // ===================================================================
    // 1. session lifecycle
    // ===================================================================
    [Fact]
    public async Task Staff_runs_full_session_lifecycle()
    {
        var f = WithFake(new FakeVisionAi());
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var sessionId = await StartSessionAsync(teacher);

        var get = await teacher.GetAsync($"/api/v1/vision/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Active", (await DataAsync(get)).GetProperty("status").GetString());

        Assert.Equal(HttpStatusCode.OK, (await AnalyzeAsync(teacher, sessionId)).StatusCode);

        var frames = await teacher.GetAsync($"/api/v1/vision/sessions/{sessionId}/frames");
        Assert.True((await DataAsync(frames)).GetArrayLength() >= 1);

        var end = await teacher.PostAsync($"/api/v1/vision/sessions/{sessionId}/end", null);
        Assert.Equal(HttpStatusCode.OK, end.StatusCode);
        Assert.Equal("Ended", (await DataAsync(end)).GetProperty("status").GetString());

        // analyzing an ended session is rejected
        Assert.Equal(HttpStatusCode.Conflict, (await AnalyzeAsync(teacher, sessionId)).StatusCode);
    }

    // ===================================================================
    // 2. role authorization
    // ===================================================================
    [Fact]
    public async Task Student_is_denied_vision_session_403()
    {
        var client = await AuthedAsync(_factory, "STU-T1");
        var resp = await client.PostAsJsonAsync("/api/v1/vision/sessions", new { title = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Parent_is_denied_vision_session_403()
    {
        var client = await AuthedAsync(_factory, "PH10-PARENT-T1");
        var resp = await client.GetAsync("/api/v1/vision/sessions");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_vision_read_401()
    {
        var resp = await TestClient.NewClient(_factory).GetAsync("/api/v1/vision/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ===================================================================
    // 3. AI unavailable -> 502 (honest, never a fake success)
    // ===================================================================
    [Fact]
    public async Task Ai_unavailable_returns_502()
    {
        var f = WithFake(new FakeVisionAi { ThrowUnavailable = true });
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var sessionId = await StartSessionAsync(teacher);
        var resp = await AnalyzeAsync(teacher, sessionId);
        Assert.Equal(HttpStatusCode.BadGateway, resp.StatusCode);
    }

    // ===================================================================
    // 4. frame persistence + candidate creation
    // ===================================================================
    [Fact]
    public async Task Analyze_persists_frame_and_creates_candidates()
    {
        var f = WithFake(new FakeVisionAi());
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var sessionId = await StartSessionAsync(teacher);

        var analyze = await AnalyzeAsync(teacher, sessionId);
        Assert.Equal(HttpStatusCode.OK, analyze.StatusCode);
        var data = await DataAsync(analyze);
        Assert.Equal(2, data.GetProperty("facesDetected").GetInt32());
        Assert.Equal("stub", data.GetProperty("engine").GetString());
        Assert.True(data.GetProperty("degraded").GetBoolean());
        Assert.All(data.GetProperty("results").EnumerateArray(),
            r => Assert.Equal("NotReady", r.GetProperty("engagement").GetString()));  // single frame can never be engaged

        var candidates = await CandidatesAsync(teacher, sessionId);
        Assert.Equal(2, candidates.Count);
        Assert.All(candidates, c => Assert.Equal("Pending", c.GetProperty("reviewStatus").GetString()));
    }

    [Fact]
    public async Task Engagement_label_persists_when_sequence_ready()
    {
        var f = WithFake(new FakeVisionAi { OnAnalyze = r => TwoFaces(r, engagement: "Engaged") });
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var sessionId = await StartSessionAsync(teacher);
        var data = await DataAsync(await AnalyzeAsync(teacher, sessionId));
        var first = data.GetProperty("results").EnumerateArray().First();
        Assert.Equal("Engaged", first.GetProperty("engagement").GetString());

        var summary = await teacher.GetAsync($"/api/v1/vision/sessions/{sessionId}/summary");
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        Assert.True((await DataAsync(summary)).GetProperty("engagedObservations").GetInt32() >= 1);
    }

    // ===================================================================
    // 5. confirm / idempotency
    // ===================================================================
    [Fact]
    public async Task Confirm_creates_cv_attendance_and_is_idempotent()
    {
        var f = WithFake(new FakeVisionAi());
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var stuT1 = await StudentIdAsync("STU-T1");
        var sessionId = await StartSessionAsync(teacher);
        await AnalyzeAsync(teacher, sessionId);
        var candidates = await CandidatesAsync(teacher, sessionId);
        var c1 = candidates[0].GetProperty("id").GetString()!;
        var c2 = candidates[1].GetProperty("id").GetString()!;

        // confirm candidate 1 -> STU-T1
        var confirm = await teacher.PostAsJsonAsync($"/api/v1/vision/candidates/{c1}/confirm", new { studentId = stuT1, status = "Present" });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal("Confirmed", (await DataAsync(confirm)).GetProperty("reviewStatus").GetString());

        // confirming the same candidate again is a conflict (idempotent review)
        var again = await teacher.PostAsJsonAsync($"/api/v1/vision/candidates/{c1}/confirm", new { studentId = stuT1 });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        // confirming a SECOND candidate to the SAME student must NOT create a duplicate
        // attendance row (same student + date + cv session key) — it updates in place.
        var confirm2 = await teacher.PostAsJsonAsync($"/api/v1/vision/candidates/{c2}/confirm", new { studentId = stuT1 });
        Assert.Equal(HttpStatusCode.OK, confirm2.StatusCode);

        // the confirmed CV attendance is visible to the student, exactly once for this session
        var student = await AuthedAsync(_factory, "STU-T1");
        var att = await student.GetAsync("/api/v1/student/attendance");
        Assert.Equal(HttpStatusCode.OK, att.StatusCode);
        var records = (await DataAsync(att)).GetProperty("records").EnumerateArray()
            .Where(r => r.GetProperty("source").GetString() == "ComputerVision"
                     && r.GetProperty("sessionKey").GetString() == $"cv-{sessionId}").ToList();
        Assert.Single(records);
    }

    [Fact]
    public async Task Confirm_unknown_candidate_without_student_is_rejected()
    {
        var f = WithFake(new FakeVisionAi());
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var sessionId = await StartSessionAsync(teacher);
        await AnalyzeAsync(teacher, sessionId);
        var candidates = await CandidatesAsync(teacher, sessionId);
        // no enrollment mapping -> mappedStudentId is null -> confirm requires an explicit student
        var lowConf = candidates.First(c => c.GetProperty("mappedStudentId").ValueKind == JsonValueKind.Null);
        var resp = await teacher.PostAsJsonAsync($"/api/v1/vision/candidates/{lowConf.GetProperty("id").GetString()}/confirm", new { });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ===================================================================
    // 6. reject / override
    // ===================================================================
    [Fact]
    public async Task Reject_then_override_candidate()
    {
        var f = WithFake(new FakeVisionAi());
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var stuT1 = await StudentIdAsync("STU-T1");
        var sessionId = await StartSessionAsync(teacher);
        await AnalyzeAsync(teacher, sessionId);
        var candidates = await CandidatesAsync(teacher, sessionId);

        var reject = await teacher.PostAsJsonAsync($"/api/v1/vision/candidates/{candidates[0].GetProperty("id").GetString()}/reject", new { notes = "not a student" });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        Assert.Equal("Rejected", (await DataAsync(reject)).GetProperty("reviewStatus").GetString());

        var over = await teacher.PostAsJsonAsync($"/api/v1/vision/candidates/{candidates[1].GetProperty("id").GetString()}/override", new { studentId = stuT1, status = "Late" });
        Assert.Equal(HttpStatusCode.OK, over.StatusCode);
        var od = await DataAsync(over);
        Assert.Equal("Overridden", od.GetProperty("reviewStatus").GetString());
        Assert.Equal("Late", od.GetProperty("resolvedStatus").GetString());
    }

    // ===================================================================
    // 7. tenant isolation
    // ===================================================================
    [Fact]
    public async Task Cross_tenant_session_not_visible_404()
    {
        var f = WithFake(new FakeVisionAi());
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var sessionId = await StartSessionAsync(teacher);

        var (otherCode, _) = await CreateStaffAsync("tenant-2", "SchoolAdmin");
        var otherAdmin = await AuthedAsync(_factory, otherCode);
        var resp = await otherAdmin.GetAsync($"/api/v1/vision/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        // and the tenant-2 admin cannot confirm a tenant-1 candidate
        var t = await AuthedAsync(f, "TEACH-T1");
        await AnalyzeAsync(t, sessionId);
        var candidates = await CandidatesAsync(t, sessionId);
        var conf = await otherAdmin.PostAsJsonAsync($"/api/v1/vision/candidates/{candidates[0].GetProperty("id").GetString()}/reject", new { });
        Assert.Equal(HttpStatusCode.NotFound, conf.StatusCode);
    }

    // ===================================================================
    // 8. audit
    // ===================================================================
    [Fact]
    public async Task Confirm_writes_audit_record()
    {
        var f = WithFake(new FakeVisionAi());
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var stuT1 = await StudentIdAsync("STU-T1");
        var sessionId = await StartSessionAsync(teacher);
        await AnalyzeAsync(teacher, sessionId);
        var candidates = await CandidatesAsync(teacher, sessionId);
        var cid = candidates[0].GetProperty("id").GetString()!;
        await teacher.PostAsJsonAsync($"/api/v1/vision/candidates/{cid}/confirm", new { studentId = stuT1 });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DerasaXDbContext>();
        var audited = await db.Set<AuditLog>().IgnoreQueryFilters()
            .AnyAsync(a => a.EntityType == "AttendanceDetectionCandidate" && a.EntityId == cid);
        Assert.True(audited, "expected an audit log for the candidate confirmation");
    }

    // ===================================================================
    // 9. enrollment-based identity mapping
    // ===================================================================
    [Fact]
    public async Task Enrollment_maps_external_label_to_student()
    {
        var ext = "ext-ENR-" + Guid.NewGuid().ToString("N")[..6];
        var f = WithFake(new FakeVisionAi
        {
            OnAnalyze = r => new AiVisionAnalyzeResponse
            {
                CorrelationId = r.CorrelationId, SessionId = r.SessionId, FacesDetected = 1, Engine = "stub", Degraded = true,
                ModelVersion = "stub-cv-2026.06", SequenceLength = 16, GeneratedAt = "2026-06-28T00:00:00Z",
                Results = new() { new AiVisionFaceResult { TrackId = ext, ExternalLabelId = ext, Bbox = new() { 0, 0, 1, 1 },
                    RecognitionConfidence = 0.9, RecognitionStatus = "candidate", Emotion = "Happy", EmotionConfidence = 0.8,
                    Engagement = "NotReady", EngagementConfidence = 0.0, EngagementFrames = 1, EngagementFramesRequired = 16 } }
            }
        });
        var teacher = await AuthedAsync(f, "TEACH-T1");
        var stuT1 = await StudentIdAsync("STU-T1");

        var enroll = await teacher.PostAsJsonAsync("/api/v1/vision/enrollments", new { studentId = stuT1, externalLabelId = ext, displayLabel = "front row" });
        Assert.Equal(HttpStatusCode.Created, enroll.StatusCode);

        var sessionId = await StartSessionAsync(teacher);
        var data = await DataAsync(await AnalyzeAsync(teacher, sessionId));
        var face = data.GetProperty("results").EnumerateArray().First();
        Assert.Equal(stuT1, face.GetProperty("mappedStudentId").GetString());  // pre-mapped via enrollment

        var candidates = await CandidatesAsync(teacher, sessionId);
        Assert.Equal(stuT1, candidates[0].GetProperty("mappedStudentId").GetString());
    }

    // ===================================================================
    // 10. student own-data + parent linked-child only
    // ===================================================================
    [Fact]
    public async Task Student_sees_only_own_engagement_summary()
    {
        var stuT1 = await StudentIdAsync("STU-T1");
        var student = await AuthedAsync(_factory, "STU-T1");
        var resp = await student.GetAsync("/api/v1/student/vision/engagement-summary");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(stuT1, (await DataAsync(resp)).GetProperty("studentId").GetString());
    }

    [Fact]
    public async Task Parent_sees_linked_child_summary_only()
    {
        var stuT1 = await StudentIdAsync("STU-T1");
        var other = await StudentIdAsync("PH8-OTHER-T1");
        var stuT2 = await StudentIdAsync("STU-T2");
        var parent = await AuthedAsync(_factory, "PH10-PARENT-T1");

        var linked = await parent.GetAsync($"/api/v1/parent/vision/children/{stuT1}/engagement-summary");
        Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        Assert.Equal(stuT1, (await DataAsync(linked)).GetProperty("studentId").GetString());

        // same-tenant but unlinked child -> 403
        Assert.Equal(HttpStatusCode.Forbidden,
            (await parent.GetAsync($"/api/v1/parent/vision/children/{other}/engagement-summary")).StatusCode);

        // cross-tenant child -> 404
        Assert.Equal(HttpStatusCode.NotFound,
            (await parent.GetAsync($"/api/v1/parent/vision/children/{stuT2}/engagement-summary")).StatusCode);
    }
}
