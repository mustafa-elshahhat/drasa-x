using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// SchoolAdmin Teacher-portal removal — backend authorization proof. SchoolAdmin no longer has
/// any /app/teacher/* page, route, or nav entry (frontend change); this suite proves the
/// backend independently enforces the same boundary regardless of the UI: SchoolAdmin now gets
/// 403 on every Teacher-personal endpoint tightened from TeacherOrSchoolAdmin to TeacherOnly
/// (curriculum authoring: Subjects/Units/Lessons/LessonMaterial; homework authoring), Teacher
/// retains full access, SchoolAdmin's own school-admin endpoints are unaffected, and other
/// roles remain blocked. The equivalent proofs for TeacherController, AiQuizController and
/// ClassroomVisionController — the other three endpoints converted to TeacherOnly in this same
/// pass — live in their existing, more contextual suites rather than being duplicated here:
/// see TeacherPortalApiTests.cs (SchoolAdmin_dashboard_is_forbidden_403 etc.),
/// AiQuizDraftApiTests.cs (SchoolAdmin_generate_draft_is_forbidden_403), and
/// Phase15ComputerVisionApiTests.cs (SchoolAdmin_is_denied_vision_session_403).
///
/// TeacherOrSchoolAdmin endpoints deliberately LEFT UNCHANGED (reviewed, not converted) —
/// GradesController (Teacher_can_write_grade/SchoolAdmin_can_write_grade already prove this is
/// genuinely dual-use — AuthorizationMatrixTests.cs), QuizzesController/QuizGradingController/
/// legacy QuizController (SchoolAdmin has a documented, heavily-tested tenant-wide bypass of
/// per-teacher subject-assignment scoping in QuizAuthoringService — AssessmentLifecycleApiTests.cs
/// — a genuine shared administrative capability, not a Teacher-portal-page leak: SchoolAdmin has
/// no UI path to it either way once the Teacher-portal nav/route is removed), AiAnalysisController/
/// AiDocumentsController/AiPredictionController's learning-profile action (explicit "teacher or
/// school admin" shared-by-design doc comments, zero frontend consumer for either role).
/// </summary>
public class SchoolAdminTeacherPortalRestrictionTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public SchoolAdminTeacherPortalRestrictionTests(IntegrationFactory factory) => _factory = factory;

    private Task<HttpClient> Admin() => TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
    private Task<HttpClient> Teacher() => TestClient.AuthedClientAsync(_factory, "TEACH-T1");

    private static async Task<JsonElement> DataAsync(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    private static MultipartFormDataContent Multipart(params (string key, string value)[] fields)
    {
        var form = new MultipartFormDataContent();
        foreach (var (k, v) in fields) form.Add(new StringContent(v), k);
        return form;
    }

    // ---- Subjects (curriculum authoring) ----

    [Fact]
    public async Task SchoolAdmin_cannot_add_subject_403()
    {
        var admin = await Admin();
        var resp = await admin.PostAsync("/api/v1/Subjects/AddSubject", Multipart(("Name", "X"), ("GradeId", "G7-ID")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Teacher_can_add_update_and_delete_subject()
    {
        var teacher = await Teacher();
        var name = "Subj-" + Guid.NewGuid().ToString("N")[..8];
        var create = await teacher.PostAsync("/api/v1/Subjects/AddSubject", Multipart(("Name", name), ("GradeId", "G7-ID")));
        Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
        var id = (await DataAsync(create)).GetProperty("id").GetString()!;
        try
        {
            var update = await teacher.PutAsync("/api/v1/Subjects/UpdateSubject",
                Multipart(("Id", id), ("Name", name + "-updated"), ("GradeId", "G7-ID")));
            Assert.True(update.IsSuccessStatusCode, await update.Content.ReadAsStringAsync());

            var delete = await teacher.DeleteAsync($"/api/v1/Subjects/DeleteSubject/{id}");
            Assert.True(delete.IsSuccessStatusCode, await delete.Content.ReadAsStringAsync());
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            db.subjects.RemoveRange(await db.subjects.IgnoreQueryFilters().Where(s => s.Id == id).ToListAsync());
            await db.SaveChangesAsync();
        }
    }

    // ---- Units / Lessons / LessonMaterial (curriculum authoring) ----

    [Fact]
    public async Task SchoolAdmin_cannot_add_unit_403()
    {
        var admin = await Admin();
        var resp = await admin.PostAsync("/api/v1/Units/AddUnit", Multipart(("Title", "X"), ("SubjectId", "PH8-SUBJECT-T1")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SchoolAdmin_cannot_add_lesson_403()
    {
        var admin = await Admin();
        var resp = await admin.PostAsync("/api/v1/Lessons/AddLesson", Multipart(("Title", "X"), ("Content", "Y"), ("UnitId", "no-such-unit")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SchoolAdmin_cannot_add_material_403()
    {
        var admin = await Admin();
        var resp = await admin.PostAsync("/api/v1/LessonMaterial/AddMaterial",
            Multipart(("LessonId", "no-such-lesson"), ("Title", "X"), ("Url", "https://example.com/x"), ("Type", "2")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Teacher_can_author_full_curriculum_chain_unit_lesson_material()
    {
        var teacher = await Teacher();
        string? unitId = null, lessonId = null;
        try
        {
            var unit = await teacher.PostAsync("/api/v1/Units/AddUnit",
                Multipart(("Title", "U-" + Guid.NewGuid().ToString("N")[..8]), ("SubjectId", "PH8-SUBJECT-T1")));
            Assert.True(unit.IsSuccessStatusCode, await unit.Content.ReadAsStringAsync());
            unitId = (await DataAsync(unit)).GetProperty("id").GetString()!;

            var lesson = await teacher.PostAsync("/api/v1/Lessons/AddLesson",
                Multipart(("Title", "L-" + Guid.NewGuid().ToString("N")[..8]), ("Content", "Body"), ("UnitId", unitId)));
            Assert.True(lesson.IsSuccessStatusCode, await lesson.Content.ReadAsStringAsync());
            lessonId = (await DataAsync(lesson)).GetProperty("id").GetString()!;

            var material = await teacher.PostAsync("/api/v1/LessonMaterial/AddMaterial",
                Multipart(("LessonId", lessonId), ("Title", "M-" + Guid.NewGuid().ToString("N")[..8]), ("Url", "https://example.com/x"), ("Type", "2")));
            Assert.True(material.IsSuccessStatusCode, await material.Content.ReadAsStringAsync());
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            if (lessonId is not null)
            {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"lessonMaterials\" WHERE \"LessonId\" = {0}", lessonId);
                db.lessons.RemoveRange(await db.lessons.IgnoreQueryFilters().Where(l => l.Id == lessonId).ToListAsync());
            }
            if (unitId is not null)
                db.units.RemoveRange(await db.units.IgnoreQueryFilters().Where(u => u.Id == unitId).ToListAsync());
            await db.SaveChangesAsync();
        }
    }

    // ---- Homework (Teacher-owned lifecycle) ----

    [Fact]
    public async Task SchoolAdmin_cannot_create_homework_403()
    {
        var admin = await Admin();
        var resp = await admin.PostAsJsonAsync("/api/v1/homework", new { title = "X" });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SchoolAdmin_cannot_list_own_homework_403()
    {
        var admin = await Admin();
        var resp = await admin.GetAsync("/api/v1/homework");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Teacher_can_create_homework()
    {
        var teacher = await Teacher();
        var create = await teacher.PostAsJsonAsync("/api/v1/homework", new { title = "HW-" + Guid.NewGuid().ToString("N")[..8] });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await DataAsync(create)).GetProperty("id").GetString()!;

        await using var db = Phase4Db.Platform(_factory);
        db.assignments.RemoveRange(await db.assignments.IgnoreQueryFilters().Where(a => a.Id == id).ToListAsync());
        await db.SaveChangesAsync();
    }

    // ---- SchoolAdmin's own endpoints are unaffected; tenant/role isolation still holds ----

    [Fact]
    public async Task SchoolAdmin_can_still_call_school_admin_dashboard()
    {
        var admin = await Admin();
        var resp = await admin.GetAsync("/api/v1/school-admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Student_and_parent_remain_blocked_from_teacher_only_endpoints()
    {
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var subjectResp = await student.PostAsync("/api/v1/Subjects/AddSubject", Multipart(("Name", "X"), ("GradeId", "G7-ID")));
        Assert.Equal(HttpStatusCode.Forbidden, subjectResp.StatusCode);

        var parent = await TestClient.AuthedClientAsync(_factory, "PH10-PARENT-T1");
        var homeworkResp = await parent.PostAsJsonAsync("/api/v1/homework", new { title = "X" });
        Assert.Equal(HttpStatusCode.Forbidden, homeworkResp.StatusCode);
    }
}
