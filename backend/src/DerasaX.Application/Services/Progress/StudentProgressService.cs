using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ProgressDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Authorization;
using DerasaX.Application.Services.Abstractions.Progress;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;

namespace DerasaX.Application.Services.Progress
{
    /// <summary>
    /// Per-student progress/insight reads. Every entry point is relationship-authorized via
    /// <see cref="IStudentAccessAuthorizer"/>. Reads NEVER execute AI — insight/prediction
    /// endpoints return STORED records only (Phase 6 owns generation). Empty data returns a
    /// valid empty result, never fabricated/demo records.
    /// </summary>
    public class StudentProgressService : IStudentProgressService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;
        private readonly IStudentAccessAuthorizer _access;
        private readonly UserManager<ApplicationUser> _users;

        public StudentProgressService(IUnitOfWork uow, ITenantContext tenant, IStudentAccessAuthorizer access, UserManager<ApplicationUser> users)
        {
            _uow = uow;
            _tenant = tenant;
            _access = access;
            _users = users;
        }

        public async Task<PaginationResponse<IEnumerable<LessonProgressDto>>> LessonProgressAsync(string studentId, ProgressParameters p, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var (from, to) = ValidateRange(p);
            Expression<Func<StudentLessonProgress, bool>> criteria = x =>
                x.StudentId == studentId &&
                (from == null || x.LastAccessedAt >= from) && (to == null || x.LastAccessedAt <= to);

            var repo = _uow.Repository<StudentLessonProgress, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<StudentLessonProgress, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<StudentLessonProgress, string>(criteria, x => x.LastAccessedAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(x => new LessonProgressDto
            {
                Id = x.Id, LessonId = x.LessonId, IsCompleted = x.IsCompleted,
                CompletionPercentage = x.CompletionPercentage, TimeSpentSeconds = x.TimeSpentSeconds,
                StartedAt = x.StartedAt, CompletedAt = x.CompletedAt, LastAccessedAt = x.LastAccessedAt
            }).ToList();
            return Page(dto, total, p);
        }

        public async Task<ApiResponse<IEnumerable<SubjectProgressDto>>> SubjectProgressAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var items = await _uow.Repository<SubjectProgress, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<SubjectProgress, string>(x => x.StudentId == studentId));
            var dto = items.Select(x => new SubjectProgressDto
            {
                Id = x.Id, SubjectId = x.SubjectId, CompletionPercentage = x.CompletionPercentage,
                AverageScore = x.AverageScore, LessonsCompleted = x.LessonsCompleted,
                TotalLessons = x.TotalLessons, LastActivityAt = x.LastActivityAt
            }).ToList();
            return Ok<IEnumerable<SubjectProgressDto>>(dto);
        }

        public async Task<ApiResponse<ProgressSummaryDto>> SummaryAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);

            var lessons = (await _uow.Repository<StudentLessonProgress, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentLessonProgress, string>(x => x.StudentId == studentId))).ToList();
            var subjects = await _uow.Repository<SubjectProgress, string>().CountAsync(
                new CriteriaSpecification<SubjectProgress, string>(x => x.StudentId == studentId));
            var attempts = (await _uow.Repository<QuizSubmission, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<QuizSubmission, string>(x =>
                    x.StudentId == studentId && x.submissionStatus != SubmissionStatus.InProgress))).ToList();

            var summary = new ProgressSummaryDto
            {
                StudentId = studentId,
                LessonsTracked = lessons.Count,
                LessonsCompleted = lessons.Count(l => l.IsCompleted),
                AverageLessonCompletion = lessons.Count == 0 ? 0 : Math.Round(lessons.Average(l => l.CompletionPercentage), 2),
                QuizAttempts = attempts.Count,
                AverageQuizPercentage = attempts.Count == 0 ? 0 : (decimal)Math.Round(attempts.Average(a => a.Percentage), 2),
                SubjectsTracked = subjects
            };
            return Ok(summary);
        }

        public async Task<PaginationResponse<IEnumerable<MetricHistoryDto>>> MetricHistoryAsync(string studentId, MetricHistoryParameters p, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var (from, to) = ValidateRange(p);
            Expression<Func<StudentMetricHistory, bool>> criteria = x =>
                x.StudentId == studentId &&
                (!p.MetricType.HasValue || x.MetricType == p.MetricType.Value) &&
                (from == null || x.MeasuredAt >= from) && (to == null || x.MeasuredAt <= to);

            var repo = _uow.Repository<StudentMetricHistory, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<StudentMetricHistory, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<StudentMetricHistory, string>(criteria, x => x.MeasuredAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(x => new MetricHistoryDto
            {
                Id = x.Id, MetricType = x.MetricType, Value = x.Value, MeasuredAt = x.MeasuredAt,
                SourceEntityType = x.SourceEntityType, SourceEntityId = x.SourceEntityId
            }).ToList();
            return Page(dto, total, p);
        }

        public async Task<PaginationResponse<IEnumerable<AttemptHistoryDto>>> AttemptHistoryAsync(string studentId, ProgressParameters p, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var (from, to) = ValidateRange(p);
            Expression<Func<QuizSubmission, bool>> criteria = x =>
                x.StudentId == studentId && x.submissionStatus != SubmissionStatus.InProgress &&
                (from == null || x.SubmittedAt >= from) && (to == null || x.SubmittedAt <= to);

            var repo = _uow.Repository<QuizSubmission, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<QuizSubmission, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<QuizSubmission, string>(criteria, x => x.SubmittedAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(x => new AttemptHistoryDto
            {
                Id = x.Id, QuizId = x.QuizId, AttemptNumber = x.AttemptNumber, Status = x.submissionStatus,
                AchievedScore = x.AchievedScore, TotalScore = x.TotalScore, Percentage = x.Percentage, SubmittedAt = x.SubmittedAt
            }).ToList();
            return Page(dto, total, p);
        }

        public async Task<ApiResponse<IEnumerable<StudentInsightDto>>> InsightsAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var items = await _uow.Repository<StudentInsight, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentInsight, string>(x => x.StudentId == studentId));
            var dto = items.Select(x => new StudentInsightDto
            {
                Id = x.Id, Performance = x.Performance, ConfidenceScore = x.ConfidenceScore, Summary = x.Summary,
                Period = x.Period, PeriodStart = x.PeriodStart, PeriodEnd = x.PeriodEnd, GeneratedAt = x.GeneratedAt
            }).ToList();
            return Ok<IEnumerable<StudentInsightDto>>(dto);
        }

        public async Task<ApiResponse<IEnumerable<PainPointDto>>> PainPointsAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var items = await _uow.Repository<PainPoint, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<PainPoint, string>(x => x.StudentId == studentId));
            var dto = items.Select(x => new PainPointDto
            {
                Id = x.Id, Category = x.Category, Title = x.Title, Description = x.Description,
                ConfidenceScore = x.ConfidenceScore, IsResolved = x.IsResolved, DetectedAt = x.DetectedAt
            }).ToList();
            return Ok<IEnumerable<PainPointDto>>(dto);
        }

        public async Task<ApiResponse<IEnumerable<RecommendationDto>>> RecommendationsAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var items = await _uow.Repository<StudentRecommendation, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentRecommendation, string>(x => x.StudentId == studentId));
            var dto = items.Select(x => new RecommendationDto
            {
                Id = x.Id, Title = x.Title, Body = x.Body, Status = x.Status, GeneratedAt = x.GeneratedAt, DueAt = x.DueAt
            }).ToList();
            return Ok<IEnumerable<RecommendationDto>>(dto);
        }

        public async Task<ApiResponse<IEnumerable<PredictionDto>>> PredictionsAsync(string studentId, CancellationToken ct = default)
        {
            await _access.EnsureCanAccessStudentAsync(studentId, ct);
            var items = await _uow.Repository<PredictionRecord, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<PredictionRecord, string>(x => x.StudentId == studentId));
            var dto = items.Select(x => new PredictionDto
            {
                Id = x.Id, Kind = x.Kind, PredictedScore = x.PredictedScore, Level = x.Level,
                ConfidenceScore = x.ConfidenceScore, ModelName = x.ModelName, ModelVersion = x.ModelVersion, PredictedAt = x.PredictedAt
            }).ToList();
            return Ok<IEnumerable<PredictionDto>>(dto);
        }

        public async Task<ApiResponse<LessonCompletionDto>> CompleteLessonAsync(string lessonId, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var studentId = _tenant.UserId ?? throw new UnauthorizedException("Authenticated student context is required.");

            var student = await _users.FindByIdAsync(studentId);
            if (student is not Student s || student.TenantId != tenantId || student.IsDeleted)
                throw new NotFoundException("Student not found.");

            var lesson = await _uow.Repository<Lesson, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Lesson, string>(x => x.Id == lessonId));
            if (lesson is null)
                throw new NotFoundException("Lesson not found.");

            var unit = await _uow.Repository<Unit, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Unit, string>(x => x.Id == lesson.UnitId));
            if (unit is null)
                throw new NotFoundException("Lesson not found.");

            var subject = await _uow.Repository<Subject, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Subject, string>(x => x.Id == unit.SubjectId));
            if (subject is null || subject.GradeId != s.GradeId)
                throw new NotFoundException("Lesson not found.");

            var enrollments = await _uow.Repository<Enrollment, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Enrollment, string>(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active));
            var classIds = enrollments.Select(e => e.SchoolClassId).Distinct().ToList();
            if (classIds.Count == 0)
                throw new NotFoundException("Lesson not found.");

            var classes = await _uow.Repository<SchoolClass, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<SchoolClass, string>(c => classIds.Contains(c.Id) && c.GradeId == subject.GradeId));
            if (!classes.Any())
                throw new NotFoundException("Lesson not found.");

            var repo = _uow.Repository<StudentLessonProgress, string>();
            var existing = await repo.GetByIdWithSpecAsync(new CriteriaSpecification<StudentLessonProgress, string>(x =>
                x.StudentId == studentId && x.LessonId == lessonId));

            var now = DateTime.UtcNow;
            var created = false;
            var progress = existing;
            if (progress is null)
            {
                created = true;
                progress = new StudentLessonProgress
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    StudentId = studentId,
                    LessonId = lessonId,
                    StartedAt = now,
                    LastAccessedAt = now
                };
                await repo.AddAsync(progress);
            }

            if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = now;
                progress.CompletionPercentage = 100;
            }
            progress.LastAccessedAt = now;

            if (!created)
                repo.Update(progress);

            await _uow.SaveChangesAsync(ct);
            await RefreshSubjectProgressAsync(studentId, subject.Id, ct);

            return Ok(new LessonCompletionDto
            {
                Id = progress.Id,
                LessonId = progress.LessonId,
                IsCompleted = progress.IsCompleted,
                CompletionPercentage = progress.CompletionPercentage,
                TimeSpentSeconds = progress.TimeSpentSeconds,
                StartedAt = progress.StartedAt,
                CompletedAt = progress.CompletedAt,
                LastAccessedAt = progress.LastAccessedAt,
                Created = created
            });
        }

        // ---- helpers ----

        private static (DateTime? from, DateTime? to) ValidateRange(ProgressParameters p)
        {
            DateTime? from = p.From.HasValue ? AsUtc(p.From.Value) : null;
            DateTime? to = p.To.HasValue ? AsUtc(p.To.Value) : null;
            if (from.HasValue && to.HasValue && to < from)
                throw new BadRequestException("'To' must be on or after 'From'.");
            return (from, to);
        }

        private static DateTime AsUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);

        private static ApiResponse<T> Ok<T>(T data) => new(data) { Success = true, StatusCode = 200, Message = "Records retrieved successfully." };

        private string RequireTenant() => _tenant.TenantId ?? throw new UnauthorizedException("Tenant context is required for this operation.");

        private async Task RefreshSubjectProgressAsync(string studentId, string subjectId, CancellationToken ct)
        {
            var units = (await _uow.Repository<Unit, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Unit, string>(u => u.SubjectId == subjectId))).ToList();
            var unitIds = units.Select(u => u.Id).ToList();
            var lessons = unitIds.Count == 0
                ? new List<Lesson>()
                : (await _uow.Repository<Lesson, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<Lesson, string>(l => unitIds.Contains(l.UnitId)))).ToList();

            var lessonIds = lessons.Select(l => l.Id).ToList();
            var completed = lessonIds.Count == 0
                ? 0
                : await _uow.Repository<StudentLessonProgress, string>().CountAsync(
                    new CriteriaSpecification<StudentLessonProgress, string>(p =>
                        p.StudentId == studentId && p.IsCompleted && lessonIds.Contains(p.LessonId)));

            var repo = _uow.Repository<SubjectProgress, string>();
            var subjectProgress = await repo.GetByIdWithSpecAsync(new CriteriaSpecification<SubjectProgress, string>(x =>
                x.StudentId == studentId && x.SubjectId == subjectId));

            var percentage = lessons.Count == 0 ? 0 : Math.Round((decimal)completed / lessons.Count * 100m, 2);
            if (subjectProgress is null)
            {
                await repo.AddAsync(new SubjectProgress
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = RequireTenant(),
                    StudentId = studentId,
                    SubjectId = subjectId,
                    TotalLessons = lessons.Count,
                    LessonsCompleted = completed,
                    CompletionPercentage = percentage,
                    LastActivityAt = DateTime.UtcNow
                });
            }
            else
            {
                subjectProgress.TotalLessons = lessons.Count;
                subjectProgress.LessonsCompleted = completed;
                subjectProgress.CompletionPercentage = percentage;
                subjectProgress.LastActivityAt = DateTime.UtcNow;
                repo.Update(subjectProgress);
            }

            await _uow.SaveChangesAsync(ct);
        }

        private static PaginationResponse<IEnumerable<T>> Page<T>(IEnumerable<T> items, int total, PaginationParameters p) =>
            new(items, total, p.PageNumber, p.PageSize) { Success = true, StatusCode = 200, Message = "Records retrieved successfully." };
    }

    public class StudentAttendanceService : IStudentAttendanceService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITenantContext _tenant;

        public StudentAttendanceService(IUnitOfWork uow, ITenantContext tenant)
        {
            _uow = uow;
            _tenant = tenant;
        }

        public async Task<ApiResponse<StudentAttendanceDto>> MyAttendanceAsync(ProgressParameters p, CancellationToken ct = default)
        {
            var studentId = _tenant.UserId ?? throw new UnauthorizedException("Authenticated student context is required.");
            var (from, to) = ValidateRange(p);

            Expression<Func<StudentAttendanceRecord, bool>> criteria = x =>
                x.StudentId == studentId &&
                (from == null || x.AttendanceDate >= from) &&
                (to == null || x.AttendanceDate <= to);

            var rows = (await _uow.Repository<StudentAttendanceRecord, string>().GetAllWithSpecAsync(
                new PagedSpecification<StudentAttendanceRecord, string>(criteria, x => x.AttendanceDate, p.PageNumber, p.PageSize, descending: true))).ToList();

            var summaryRows = (await _uow.Repository<StudentAttendanceRecord, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<StudentAttendanceRecord, string>(criteria))).ToList();

            var present = summaryRows.Count(x => x.Status == AttendanceStatus.Present);
            var late = summaryRows.Count(x => x.Status == AttendanceStatus.Late);
            var total = summaryRows.Count;
            var dto = new StudentAttendanceDto
            {
                Summary = new AttendanceSummaryDto
                {
                    Total = total,
                    Present = present,
                    Absent = summaryRows.Count(x => x.Status == AttendanceStatus.Absent),
                    Late = late,
                    Excused = summaryRows.Count(x => x.Status == AttendanceStatus.Excused),
                    AttendancePercentage = total == 0 ? 0 : Math.Round((decimal)(present + late) / total * 100m, 2)
                },
                Records = rows.Select(x => new AttendanceRecordDto
                {
                    Id = x.Id,
                    AttendanceDate = x.AttendanceDate,
                    Status = x.Status.ToString(),
                    RecordedAt = x.RecordedAt,
                    Source = x.Source.ToString(),
                    SessionKey = x.SessionKey,
                    SchoolClassId = x.SchoolClassId,
                    Notes = x.Notes
                }).ToList()
            };

            return new ApiResponse<StudentAttendanceDto>(dto) { Success = true, StatusCode = 200, Message = "Records retrieved successfully." };
        }

        private static (DateTime? from, DateTime? to) ValidateRange(ProgressParameters p)
        {
            DateTime? from = p.From.HasValue ? AsUtc(p.From.Value) : null;
            DateTime? to = p.To.HasValue ? AsUtc(p.To.Value) : null;
            if (from.HasValue && to.HasValue && to < from)
                throw new BadRequestException("'To' must be on or after 'From'.");
            return (from, to);
        }

        private static DateTime AsUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc);
    }
}
