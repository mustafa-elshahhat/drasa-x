using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

public class Phase8StudentContractsTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public Phase8StudentContractsTests(IntegrationFactory factory) => _factory = factory;

    private sealed record Env<T>(bool success, int statusCode, T? data);
    private sealed record AttendanceDto(AttendanceSummaryDto summary, List<AttendanceRowDto> records);
    private sealed record AttendanceSummaryDto(int total, int present, int absent, int late, int excused, decimal attendancePercentage);
    private sealed record AttendanceRowDto(string id, string status, string source, string? schoolClassId);
    private sealed record CompletionDto(string id, string lessonId, bool isCompleted, decimal completionPercentage, DateTime? completedAt, bool created);
    private sealed record LessonProgressDto(string lessonId, bool isCompleted, decimal completionPercentage);

    private sealed record World(
        string StudentId,
        string StudentLogin,
        string YearId,
        string ClassId,
        string EnrollmentId,
        string SubjectId,
        string UnitId,
        string LessonId,
        string OtherGradeId,
        string UnassignedSubjectId,
        string UnassignedUnitId,
        string UnassignedLessonId,
        string Attendance1,
        string Attendance2,
        string T2AttendanceId);

    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    private async Task<World> SetupAsync(bool addAttendance = true)
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var w = new World(
            student.Id, student.LoginCode,
            NewId("ay"), NewId("cls"), NewId("enr"), NewId("sub"), NewId("unit"), NewId("les"),
            NewId("g8"), NewId("subx"), NewId("unitx"), NewId("lesx"), NewId("att"), NewId("att"), NewId("att2"));

        await using var db = Phase4Db.Platform(_factory);
        db.grades.Add(new Grade { Id = w.OtherGradeId, TenantId = "tenant-1", Name = "Phase 8 Other Grade" });
        db.academicYears.Add(new AcademicYear { Id = w.YearId, TenantId = "tenant-1", Name = "Phase 8 Test Year", Code = NewId("AY")[..12], StartDate = U(2030, 9, 1), EndDate = U(2031, 6, 30) });
        db.schoolClasses.Add(new SchoolClass { Id = w.ClassId, TenantId = "tenant-1", Name = "Phase 8 Test Class", Code = NewId("C")[..12], GradeId = "G7-ID", AcademicYearId = w.YearId, Capacity = 30 });
        db.enrollments.Add(new Enrollment { Id = w.EnrollmentId, TenantId = "tenant-1", StudentId = w.StudentId, SchoolClassId = w.ClassId, AcademicYearId = w.YearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
        db.subjects.Add(new Subject { Id = w.SubjectId, TenantId = "tenant-1", Name = "Phase 8 Test Subject", GradeId = "G7-ID" });
        db.units.Add(new Unit { Id = w.UnitId, TenantId = "tenant-1", Title = "Phase 8 Test Unit", SubjectId = w.SubjectId });
        db.lessons.Add(new Lesson { Id = w.LessonId, TenantId = "tenant-1", Title = "Phase 8 Test Lesson", Content = "content", UnitId = w.UnitId });
        db.subjects.Add(new Subject { Id = w.UnassignedSubjectId, TenantId = "tenant-1", Name = "Phase 8 Unassigned Subject", GradeId = w.OtherGradeId });
        db.units.Add(new Unit { Id = w.UnassignedUnitId, TenantId = "tenant-1", Title = "Phase 8 Unassigned Unit", SubjectId = w.UnassignedSubjectId });
        db.lessons.Add(new Lesson { Id = w.UnassignedLessonId, TenantId = "tenant-1", Title = "Phase 8 Unassigned Lesson", Content = "content", UnitId = w.UnassignedUnitId });

        if (addAttendance)
        {
            db.studentAttendanceRecords.Add(new StudentAttendanceRecord { Id = w.Attendance1, TenantId = "tenant-1", StudentId = w.StudentId, SchoolClassId = w.ClassId, AttendanceDate = U(2031, 1, 5), RecordedAt = U(2031, 1, 5).AddHours(8), Status = AttendanceStatus.Present, Source = AttendanceSource.Manual, SessionKey = "day" });
            db.studentAttendanceRecords.Add(new StudentAttendanceRecord { Id = w.Attendance2, TenantId = "tenant-1", StudentId = w.StudentId, SchoolClassId = w.ClassId, AttendanceDate = U(2031, 1, 6), RecordedAt = U(2031, 1, 6).AddHours(8), Status = AttendanceStatus.Absent, Source = AttendanceSource.Import, SessionKey = "day" });
            var t2Student = await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == "STU-T2");
            var t2Date = DateTime.UtcNow.AddYears(20).AddTicks(Math.Abs(w.T2AttendanceId.GetHashCode()));
            db.studentAttendanceRecords.Add(new StudentAttendanceRecord { Id = w.T2AttendanceId, TenantId = "tenant-2", StudentId = t2Student.Id, AttendanceDate = t2Date, RecordedAt = t2Date.AddHours(8), Status = AttendanceStatus.Present, Source = AttendanceSource.Manual, SessionKey = "day" });
        }

        await db.SaveChangesAsync();
        return w;
    }

    private async Task Cleanup(World w)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var (table, id) in new[]
        {
            ("subjectProgresses", (string?)null), ("studentLessonProgresses", (string?)null),
            ("studentAttendanceRecords", w.Attendance1), ("studentAttendanceRecords", w.Attendance2), ("studentAttendanceRecords", w.T2AttendanceId),
            ("lessons", w.LessonId), ("lessons", w.UnassignedLessonId), ("units", w.UnitId), ("units", w.UnassignedUnitId),
            ("subjects", w.SubjectId), ("subjects", w.UnassignedSubjectId), ("enrollments", w.EnrollmentId),
            ("schoolClasses", w.ClassId), ("academicYears", w.YearId), ("grades", w.OtherGradeId)
        })
        {
#pragma warning disable EF1002
            if (id is null)
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"StudentId\" = {{0}}", w.StudentId);
            else
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"Id\" = {{0}}", id);
#pragma warning restore EF1002
        }
    }

    private static async Task<Env<T>> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Env<T>>(raw, Json)!;
    }

    [Fact]
    public async Task Attendance_endpoint_enforces_auth_role_self_context_and_summary()
    {
        var w = await SetupAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await TestClient.NewClient(_factory).GetAsync("/api/v1/student/attendance")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await (await TestClient.AuthedClientAsync(_factory, "TEACH-T1")).GetAsync("/api/v1/student/attendance")).StatusCode);

            var student = await TestClient.AuthedClientAsync(_factory, w.StudentLogin);
            var result = await Read<AttendanceDto>(await student.GetAsync("/api/v1/student/attendance?studentId=spoof&tenantId=tenant-2"));

            Assert.True(result.success);
            Assert.Equal(2, result.data!.summary.total);
            Assert.Equal(1, result.data.summary.present);
            Assert.Equal(1, result.data.summary.absent);
            Assert.Equal(50m, result.data.summary.attendancePercentage);
            Assert.All(result.data.records, r => Assert.Contains(r.id, new[] { w.Attendance1, w.Attendance2 }));
            Assert.DoesNotContain(result.data.records, r => r.id == w.T2AttendanceId || r.source == nameof(AttendanceSource.ComputerVision));
        }
        finally { await Cleanup(w); }
    }

    [Fact]
    public async Task Attendance_empty_result_and_duplicate_integrity_rule_work()
    {
        var w = await SetupAsync(addAttendance: false);
        try
        {
            var student = await TestClient.AuthedClientAsync(_factory, w.StudentLogin);
            var empty = await Read<AttendanceDto>(await student.GetAsync("/api/v1/student/attendance"));
            Assert.True(empty.success);
            Assert.Equal(0, empty.data!.summary.total);
            Assert.Empty(empty.data.records);

            await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
            db.studentAttendanceRecords.Add(new StudentAttendanceRecord { Id = w.Attendance1, TenantId = "tenant-1", StudentId = w.StudentId, SchoolClassId = w.ClassId, AttendanceDate = U(2031, 1, 5), RecordedAt = U(2031, 1, 5).AddHours(8), Status = AttendanceStatus.Present, Source = AttendanceSource.Manual, SessionKey = "day" });
            db.studentAttendanceRecords.Add(new StudentAttendanceRecord { Id = w.Attendance2, TenantId = "tenant-1", StudentId = w.StudentId, SchoolClassId = w.ClassId, AttendanceDate = U(2031, 1, 5), RecordedAt = U(2031, 1, 5).AddHours(9), Status = AttendanceStatus.Late, Source = AttendanceSource.Manual, SessionKey = "day" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally { await Cleanup(w); }
    }

    [Fact]
    public async Task Lesson_completion_is_explicit_persisted_idempotent_and_updates_read_model()
    {
        var w = await SetupAsync(addAttendance: false);
        try
        {
            var student = await TestClient.AuthedClientAsync(_factory, w.StudentLogin);

            Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/Lessons/GetLessonsByUnitId?id={w.UnitId}")).StatusCode);
            await using (var before = Phase4Db.AsTenant(_factory, "tenant-1"))
                Assert.False(await before.studentLessonProgresses.AnyAsync(p => p.StudentId == w.StudentId && p.LessonId == w.LessonId));

            var first = await Read<CompletionDto>(await student.PostAsJsonAsync($"/api/v1/student/lessons/{w.LessonId}/complete", new { studentId = "spoof", tenantId = "tenant-2" }));
            Assert.True(first.success);
            Assert.True(first.data!.isCompleted);
            Assert.Equal(100m, first.data.completionPercentage);

            var second = await Read<CompletionDto>(await student.PostAsync($"/api/v1/student/lessons/{w.LessonId}/complete", null));
            Assert.True(second.success);
            Assert.Equal(first.data.id, second.data!.id);

            await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                Assert.Equal(1, await db.studentLessonProgresses.CountAsync(p => p.StudentId == w.StudentId && p.LessonId == w.LessonId));
                var sp = await db.subjectProgresses.SingleAsync(p => p.StudentId == w.StudentId && p.SubjectId == w.SubjectId);
                Assert.Equal(1, sp.LessonsCompleted);
                Assert.Equal(100m, sp.CompletionPercentage);
            }

            var progress = await Read<List<LessonProgressDto>>(await student.GetAsync($"/api/v1/students/{w.StudentId}/lesson-progress"));
            Assert.Contains(progress.data!, p => p.lessonId == w.LessonId && p.isCompleted && p.completionPercentage == 100m);
        }
        finally { await Cleanup(w); }
    }

    [Fact]
    public async Task Lesson_completion_rejects_wrong_context_and_invisible_lessons()
    {
        var w = await SetupAsync(addAttendance: false);
        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await TestClient.NewClient(_factory).PostAsync($"/api/v1/student/lessons/{w.LessonId}/complete", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await (await TestClient.AuthedClientAsync(_factory, "TEACH-T1")).PostAsync($"/api/v1/student/lessons/{w.LessonId}/complete", null)).StatusCode);

            var student = await TestClient.AuthedClientAsync(_factory, w.StudentLogin);
            Assert.Equal(HttpStatusCode.NotFound, (await student.PostAsync($"/api/v1/student/lessons/{w.UnassignedLessonId}/complete", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await student.PostAsync("/api/v1/student/lessons/PH8-LESSON-T2/complete", null)).StatusCode);
        }
        finally { await Cleanup(w); }
    }
}
