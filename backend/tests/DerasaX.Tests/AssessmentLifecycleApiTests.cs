using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 §10 (Increment 3) — assessment lifecycle through the real HTTP pipeline against
/// local PostgreSQL: authoring, publication validation, assignment, eligibility, the attempt
/// lifecycle (start/save/submit), authoritative auto-grading, manual grading, feedback,
/// idempotency, deadline/attempt-limit enforcement, correct-answer non-disclosure, tenant
/// isolation, and the required audit/notification records.
/// </summary>
public class AssessmentLifecycleApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AssessmentLifecycleApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed record Env<T>(bool success, int statusCode, string? message, T? data, int totalCount);
    private sealed record QuizData(string id, int status);
    private sealed record QuestionData(string id);
    private sealed record OptData(string id, string text, bool? isCorrect);
    private sealed record QData(string id, int type, List<OptData> options);
    private sealed record AnsData(string questionId, string? selectedOptionId, bool? isCorrect, int? pointsEarned);
    private sealed record AttemptData(string id, int status, int achievedScore, int totalScore,
        List<QData> questions, List<AnsData> answers);
    private sealed record AssignedData(string quizId, bool canAttempt, string status, string? latestAttemptId,
        int questionCount, int timeLimitMinutes, int? score, double? percentage);
    private sealed record SubmissionData(string id, int status, int achievedScore, int totalScore);
    private sealed record AnalyticsData(string quizId, int totalSubmissions);

    private static string Code(string p) => $"{p}-{Guid.NewGuid():N}"[..14];

    private Task<HttpClient> Admin1() => TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
    private Task<HttpClient> Admin2() => TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
    private Task<HttpClient> Student1() => TestClient.AuthedClientAsync(_factory, "STU-T1");
    private Task<HttpClient> Teacher1() => TestClient.AuthedClientAsync(_factory, "TEACH-T1");

    private static async Task<(HttpStatusCode, Env<T>?)> ReadEnv<T>(HttpResponseMessage resp)
    {
        Env<T>? body = null;
        var raw = await resp.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
            try { body = JsonSerializer.Deserialize<Env<T>>(raw, Json); } catch { /* problem+json */ }
        return (resp.StatusCode, body);
    }

    private async Task<string> UserId(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    private async Task<(string quizId, List<string> questionIds)> CreatePublishedMcqQuizAsync(
        HttpClient admin, int? maxAttempts = null, int questions = 2)
    {
        var create = await admin.PostAsJsonAsync("/api/v1/quizzes", new
        {
            title = "Quiz " + Code("Q"), type = 1, difficulty = 2, timeLimitMinutes = 30, maxAttempts
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var (_, cb) = await ReadEnv<QuizData>(create);
        var quizId = cb!.data!.id;

        var qIds = new List<string>();
        for (var i = 0; i < questions; i++)
        {
            var qr = await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/questions", new
            {
                text = $"Q{i}", type = 1, order = i, points = 5,
                options = new[]
                {
                    new { text = "RIGHT", isCorrect = true },
                    new { text = "WRONG", isCorrect = false }
                }
            });
            Assert.Equal(HttpStatusCode.Created, qr.StatusCode);
            var (_, qb) = await ReadEnv<QuestionData>(qr);
            qIds.Add(qb!.data!.id);
        }

        var pub = await admin.PostAsync($"/api/v1/quizzes/{quizId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, pub.StatusCode);
        return (quizId, qIds);
    }

    private async Task<(string yearId, string classId, string enrollId)> EnrollStudentInNewClassAsync(HttpClient admin, string studentLogin)
    {
        var yr = await admin.PostAsJsonAsync("/api/v1/academic-years", new
        {
            name = "Y", code = Code("AY"), startDate = new DateTime(2030, 9, 1), endDate = new DateTime(2031, 6, 30)
        });
        var yearId = JsonSerializer.Deserialize<Env<QuizData>>(await yr.Content.ReadAsStringAsync(), Json)!.data!.id;

        var cls = await admin.PostAsJsonAsync("/api/v1/classes", new
        {
            name = "C", code = Code("C"), capacity = 30, gradeId = "G7-ID", academicYearId = yearId
        });
        var classId = JsonSerializer.Deserialize<Env<QuizData>>(await cls.Content.ReadAsStringAsync(), Json)!.data!.id;

        var enr = await admin.PostAsJsonAsync("/api/v1/enrollments", new
        {
            studentId = await UserId(studentLogin), schoolClassId = classId
        });
        Assert.Equal(HttpStatusCode.Created, enr.StatusCode);
        var enrollId = JsonSerializer.Deserialize<Env<QuizData>>(await enr.Content.ReadAsStringAsync(), Json)!.data!.id;
        return (yearId, classId, enrollId);
    }

    private async Task CleanupQuiz(string quizId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"submissionAnswers\" WHERE \"QuizSubmissionId\" IN (SELECT \"Id\" FROM \"quizSubmissions\" WHERE \"QuizId\" = {0})", quizId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"quizSubmissions\" WHERE \"QuizId\" = {0}", quizId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"assignmentTargets\" WHERE \"AssignmentId\" IN (SELECT \"Id\" FROM \"assignments\" WHERE \"QuizId\" = {0})", quizId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"assignments\" WHERE \"QuizId\" = {0}", quizId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"questionOptions\" WHERE \"QuestionId\" IN (SELECT \"Id\" FROM \"questions\" WHERE \"QuizId\" = {0})", quizId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"questions\" WHERE \"QuizId\" = {0}", quizId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"quizzes\" WHERE \"Id\" = {0}", quizId);
    }

    private async Task CleanupAcademic(string yearId, string classId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"enrollments\" WHERE \"SchoolClassId\" = {0}", classId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"schoolClasses\" WHERE \"Id\" = {0}", classId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"academicYears\" WHERE \"Id\" = {0}", yearId);
    }

    private async Task CleanupNotifications(string userId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" = {0} AND \"NotificationCategory\" IN ('QuizAssigned','QuizGraded')", userId);
    }

    // ---- Authoring, validation, authorization, tenant isolation ----

    [Fact]
    public async Task Unauthenticated_create_returns_401()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.PostAsJsonAsync("/api/v1/quizzes", new { title = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Student_cannot_author_quiz_403()
    {
        var client = await Student1();
        var resp = await client.PostAsJsonAsync("/api/v1/quizzes", new { title = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_cannot_use_tenant_quiz_route_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var resp = await client.GetAsync("/api/v1/quizzes");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Invalid_then_valid_publication_and_cross_tenant_404()
    {
        var admin = await Admin1();
        var create = await admin.PostAsJsonAsync("/api/v1/quizzes", new { title = "Draft", type = 1, timeLimitMinutes = 30 });
        var (cs, cb) = await ReadEnv<QuizData>(create);
        Assert.Equal(HttpStatusCode.Created, cs);
        Assert.Equal(0, cb!.data!.status); // Draft
        var quizId = cb.data.id;
        try
        {
            // Publish with no questions → 400.
            var emptyPub = await admin.PostAsync($"/api/v1/quizzes/{quizId}/publish", null);
            Assert.Equal(HttpStatusCode.BadRequest, emptyPub.StatusCode);

            // Add a question with no correct option → 400.
            var badQ = await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/questions", new
            {
                text = "Q", type = 1, order = 0, points = 5,
                options = new[] { new { text = "a", isCorrect = false }, new { text = "b", isCorrect = false } }
            });
            Assert.Equal(HttpStatusCode.BadRequest, badQ.StatusCode);

            // Valid question → publish OK.
            var goodQ = await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/questions", new
            {
                text = "Q", type = 1, order = 0, points = 5,
                options = new[] { new { text = "a", isCorrect = true }, new { text = "b", isCorrect = false } }
            });
            Assert.Equal(HttpStatusCode.Created, goodQ.StatusCode);

            var pub = await admin.PostAsync($"/api/v1/quizzes/{quizId}/publish", null);
            var (ps, pb) = await ReadEnv<QuizData>(pub);
            Assert.Equal(HttpStatusCode.OK, ps);
            Assert.Equal(4, pb!.data!.status); // Published

            // Editing a published quiz is rejected (historical integrity) → 409.
            var edit = await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/questions", new
            {
                text = "Q2", type = 1, order = 1, points = 5,
                options = new[] { new { text = "a", isCorrect = true }, new { text = "b", isCorrect = false } }
            });
            Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);

            // Cross-tenant read → 404 (no existence leak).
            var admin2 = await Admin2();
            var cross = await admin2.GetAsync($"/api/v1/quizzes/{quizId}");
            Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
        }
        finally { await CleanupQuiz(quizId); }
    }

    // ---- Full attempt lifecycle: assign → start → save → submit → autograde → idempotency ----

    [Fact]
    public async Task Full_attempt_lifecycle_autograde_idempotency_and_records()
    {
        var admin = await Admin1();
        var stuId = await UserId("STU-T1");
        string quizId = "", yearId = "", classId = "";
        try
        {
            (quizId, _) = await CreatePublishedMcqQuizAsync(admin);
            (yearId, classId, _) = await EnrollStudentInNewClassAsync(admin, "STU-T1");

            // Assign to the class.
            var assign = await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/assignments", new { schoolClassId = classId });
            Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

            var student = await Student1();

            // Assigned-quiz discovery.
            var assigned = await student.GetAsync("/api/v1/assigned-quizzes");
            var (asg, ab) = await ReadEnv<List<AssignedData>>(assigned);
            Assert.Equal(HttpStatusCode.OK, asg);
            Assert.Contains(ab!.data!, a => a.quizId == quizId && a.canAttempt);
            // Contract: an un-attempted assigned quiz reports a real student status + metadata.
            var beforeRow = ab.data!.First(a => a.quizId == quizId);
            Assert.Equal("available", beforeRow.status);
            Assert.Null(beforeRow.latestAttemptId);
            Assert.Equal(2, beforeRow.questionCount);
            Assert.Equal(30, beforeRow.timeLimitMinutes);

            // Start → 201, returns questions (no correct flags).
            var start = await student.PostAsync($"/api/v1/quizzes/{quizId}/attempts", null);
            var (ss, sb) = await ReadEnv<AttemptData>(start);
            Assert.Equal(HttpStatusCode.Created, ss);
            var attemptId = sb!.data!.id;
            Assert.Equal(2, sb.data.questions.Count);
            Assert.All(sb.data.questions, q => Assert.All(q.options, o => Assert.Null(o.isCorrect))); // non-disclosure

            // Duplicate start → resume the SAME attempt (idempotent).
            var start2 = await student.PostAsync($"/api/v1/quizzes/{quizId}/attempts", null);
            var (ss2, sb2) = await ReadEnv<AttemptData>(start2);
            Assert.Equal(HttpStatusCode.OK, ss2);
            Assert.Equal(attemptId, sb2!.data!.id);

            // Save answers picking the RIGHT option in each question.
            var answers = sb.data.questions.Select(q => new
            {
                questionId = q.id,
                selectedOptionId = q.options.First(o => o.text == "RIGHT").id,
                answerText = (string?)null
            }).ToArray();
            var save = await student.PutAsJsonAsync($"/api/v1/attempts/{attemptId}/answers", new { answers });
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);

            // Submit → authoritative full score.
            var submit = await student.PostAsync($"/api/v1/attempts/{attemptId}/submit", null);
            var (subS, subB) = await ReadEnv<SubmissionData>(submit);
            Assert.Equal(HttpStatusCode.OK, subS);
            Assert.Equal(2, subB!.data!.status); // Graded
            Assert.Equal(10, subB.data.achievedScore);
            Assert.Equal(10, subB.data.totalScore);

            // Duplicate submit → 409 (no duplicate grade).
            var submit2 = await student.PostAsync($"/api/v1/attempts/{attemptId}/submit", null);
            Assert.Equal(HttpStatusCode.Conflict, submit2.StatusCode);

            // Result view.
            var result = await student.GetAsync($"/api/v1/attempts/{attemptId}/result");
            var (rS, rB) = await ReadEnv<AttemptData>(result);
            Assert.Equal(HttpStatusCode.OK, rS);
            Assert.Equal(10, rB!.data!.achievedScore);

            // Contract: after a graded attempt the assigned list reports status + the latest attempt id
            // (so the UI links "View result" by attemptId, not quizId) and the achieved score/percentage.
            var assignedAfter = await student.GetAsync("/api/v1/assigned-quizzes");
            var (_, abAfter) = await ReadEnv<List<AssignedData>>(assignedAfter);
            var afterRow = abAfter!.data!.First(a => a.quizId == quizId);
            Assert.Equal("graded", afterRow.status);
            Assert.Equal(attemptId, afterRow.latestAttemptId);
            Assert.Equal(10, afterRow.score);
            Assert.Equal(100d, afterRow.percentage);

            // Teacher analytics.
            var analytics = await admin.GetAsync($"/api/v1/quizzes/{quizId}/analytics");
            var (anS, anB) = await ReadEnv<AnalyticsData>(analytics);
            Assert.Equal(HttpStatusCode.OK, anS);
            Assert.True(anB!.data!.totalSubmissions >= 1);

            // Required records: a graded notification and a submit audit row.
            await using var db = Phase4Db.Platform(_factory);
            Assert.True(await db.notifications.IgnoreQueryFilters()
                .AnyAsync(n => n.UserId == stuId && n.Title == "Quiz graded"));
            Assert.True(await db.auditLogs.IgnoreQueryFilters()
                .AnyAsync(a => a.EntityType == "QuizSubmission" && a.EntityId == attemptId));
        }
        finally
        {
            await CleanupNotifications(stuId);
            if (quizId != "") await CleanupQuiz(quizId);
            if (classId != "") await CleanupAcademic(yearId, classId);
        }
    }

    // ---- Eligibility, deadline, attempt-limit and cross-tenant assignment ----

    [Fact]
    public async Task Eligibility_deadline_attempt_limit_and_cross_tenant_assignment()
    {
        var admin = await Admin1();
        var stuId = await UserId("STU-T1");
        string quizId = "", lateQuizId = "", yearId = "", classId = "";
        try
        {
            (quizId, _) = await CreatePublishedMcqQuizAsync(admin, maxAttempts: 1, questions: 1);
            (lateQuizId, _) = await CreatePublishedMcqQuizAsync(admin, questions: 1);
            (yearId, classId, _) = await EnrollStudentInNewClassAsync(admin, "STU-T1");

            // Unassigned same-tenant student cannot attempt → 403 (no assignment yet).
            var student = await Student1();
            var earlyStart = await student.PostAsync($"/api/v1/quizzes/{quizId}/attempts", null);
            Assert.Equal(HttpStatusCode.Forbidden, earlyStart.StatusCode);

            // Assign the "late" quiz with a past due date → start → 409 (deadline).
            var lateAssign = await admin.PostAsJsonAsync($"/api/v1/quizzes/{lateQuizId}/assignments", new
            {
                schoolClassId = classId, dueDate = new DateTime(2000, 1, 1)
            });
            Assert.Equal(HttpStatusCode.Created, lateAssign.StatusCode);
            var lateStart = await student.PostAsync($"/api/v1/quizzes/{lateQuizId}/attempts", null);
            Assert.Equal(HttpStatusCode.Conflict, lateStart.StatusCode);

            // Cross-tenant student assignment target → 404 (no existence leak).
            var crossAssign = await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/assignments", new
            {
                studentIds = new[] { await UserId("STU-T2") }
            });
            Assert.Equal(HttpStatusCode.NotFound, crossAssign.StatusCode);

            // Assign the limited quiz to the class, attempt once, then exceed the limit → 409.
            var assign = await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/assignments", new { schoolClassId = classId });
            Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

            var start = await student.PostAsync($"/api/v1/quizzes/{quizId}/attempts", null);
            var (_, sb) = await ReadEnv<AttemptData>(start);
            Assert.Equal(HttpStatusCode.Created, start.StatusCode);
            var submit = await student.PostAsync($"/api/v1/attempts/{sb!.data!.id}/submit", null);
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

            var startAgain = await student.PostAsync($"/api/v1/quizzes/{quizId}/attempts", null);
            Assert.Equal(HttpStatusCode.Conflict, startAgain.StatusCode); // attempt limit
        }
        finally
        {
            await CleanupNotifications(stuId);
            if (quizId != "") await CleanupQuiz(quizId);
            if (lateQuizId != "") await CleanupQuiz(lateQuizId);
            if (classId != "") await CleanupAcademic(yearId, classId);
        }
    }

    // ---- Teacher authorization + manual grading + feedback ----

    [Fact]
    public async Task Manual_grading_feedback_and_authorization()
    {
        var admin = await Admin1();
        var stuId = await UserId("STU-T1");
        string quizId = "", yearId = "", classId = "";
        try
        {
            // Quiz with an essay question (manual grading) — no subject anchor.
            var create = await admin.PostAsJsonAsync("/api/v1/quizzes", new { title = "Essay", type = 1, timeLimitMinutes = 30 });
            quizId = JsonSerializer.Deserialize<Env<QuizData>>(await create.Content.ReadAsStringAsync(), Json)!.data!.id;
            var addQ = await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/questions", new
            {
                text = "Explain", type = 4, order = 0, points = 8, options = Array.Empty<object>()
            });
            Assert.Equal(HttpStatusCode.Created, addQ.StatusCode);
            var qId = JsonSerializer.Deserialize<Env<QuestionData>>(await addQ.Content.ReadAsStringAsync(), Json)!.data!.id;
            await admin.PostAsync($"/api/v1/quizzes/{quizId}/publish", null);

            // A teacher NOT assigned to this (subject-less) quiz cannot manage it → 403.
            var teacher = await Teacher1();
            var teacherView = await teacher.GetAsync($"/api/v1/quizzes/{quizId}");
            Assert.Equal(HttpStatusCode.Forbidden, teacherView.StatusCode);

            (yearId, classId, _) = await EnrollStudentInNewClassAsync(admin, "STU-T1");
            await admin.PostAsJsonAsync($"/api/v1/quizzes/{quizId}/assignments", new { schoolClassId = classId });

            // Student attempts the essay.
            var student = await Student1();
            var start = await student.PostAsync($"/api/v1/quizzes/{quizId}/attempts", null);
            var attemptId = JsonSerializer.Deserialize<Env<AttemptData>>(await start.Content.ReadAsStringAsync(), Json)!.data!.id;
            await student.PutAsJsonAsync($"/api/v1/attempts/{attemptId}/answers", new
            {
                answers = new[] { new { questionId = qId, selectedOptionId = (string?)null, answerText = "Because." } }
            });

            // In-progress answers never disclose correctness.
            var inProg = await student.GetAsync($"/api/v1/attempts/{attemptId}");
            var (_, ipBody) = await ReadEnv<AttemptData>(inProg);
            Assert.All(ipBody!.data!.answers, a => Assert.Null(a.isCorrect));

            var submit = await student.PostAsync($"/api/v1/attempts/{attemptId}/submit", null);
            var (subS, subB) = await ReadEnv<SubmissionData>(submit);
            Assert.Equal(HttpStatusCode.OK, subS);
            Assert.Equal(1, subB!.data!.status); // Submitted (awaiting manual grade)
            Assert.Equal(0, subB.data.achievedScore);

            // A non-assigned teacher cannot grade → 403.
            var teacherGrade = await teacher.PostAsJsonAsync($"/api/v1/submissions/{attemptId}/grade", new
            {
                grades = new[] { new { answerId = "x", pointsEarned = 1, isCorrect = true, feedback = (string?)null } }
            });
            Assert.Equal(HttpStatusCode.Forbidden, teacherGrade.StatusCode);

            // Admin reviews the submission, reads the answer id, grades it, adds feedback.
            var review = await admin.GetAsync($"/api/v1/submissions/{attemptId}");
            var (rS, rB) = await ReadEnv<AttemptData>(review);
            Assert.Equal(HttpStatusCode.OK, rS);
            var answerId = await AnswerIdFor(attemptId, qId);

            var grade = await admin.PostAsJsonAsync($"/api/v1/submissions/{attemptId}/grade", new
            {
                grades = new[] { new { answerId, pointsEarned = 8, isCorrect = true, feedback = "Good" } }
            });
            var (gS, gB) = await ReadEnv<SubmissionData>(grade);
            Assert.Equal(HttpStatusCode.OK, gS);
            Assert.Equal(2, gB!.data!.status); // Graded
            Assert.Equal(8, gB.data.achievedScore);

            var feedback = await admin.PostAsJsonAsync($"/api/v1/submissions/{attemptId}/feedback", new { feedback = "Well done" });
            Assert.Equal(HttpStatusCode.OK, feedback.StatusCode);

            // Manual-grade actions are audited.
            await using var db = Phase4Db.Platform(_factory);
            Assert.True(await db.auditLogs.IgnoreQueryFilters()
                .AnyAsync(a => a.EntityType == "QuizSubmission" && a.EntityId == attemptId
                    && a.MetadataJson != null && a.MetadataJson.Contains("manual-grade")));
        }
        finally
        {
            await CleanupNotifications(stuId);
            if (quizId != "") await CleanupQuiz(quizId);
            if (classId != "") await CleanupAcademic(yearId, classId);
        }
    }

    private async Task<string> AnswerIdFor(string attemptId, string questionId)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.submissionAnswers.IgnoreQueryFilters()
            .FirstAsync(a => a.QuizSubmissionId == attemptId && a.QuestionId == questionId)).Id;
    }
}
